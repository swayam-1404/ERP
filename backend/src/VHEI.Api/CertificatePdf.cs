using System.Reflection;
using System.Text.Json;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using QRCoder;
using VHEI.Domain;

namespace VHEI.Api;

// Fixed, versioned review template. No officer signatures or university seals are simulated.
public static class CertificatePdf
{
    private static readonly object FontLock=new();
    public static byte[] Render(AcademicCertificate certificate,string verificationUrl)
    {
        lock(FontLock){if(GlobalFontSettings.FontResolver==null)GlobalFontSettings.FontResolver=new EmbeddedFont();}
        using var snapshot=JsonDocument.Parse(certificate.SnapshotJson);var root=snapshot.RootElement;
        static string Strings(JsonElement element) => element.ValueKind switch {JsonValueKind.String=>element.GetString()!,JsonValueKind.Object=>string.Concat(element.EnumerateObject().Select(p=>Strings(p.Value))),JsonValueKind.Array=>string.Concat(element.EnumerateArray().Select(Strings)),_=>""};
        var renderedText=Strings(root)+certificate.AuthorityReference+certificate.ClearanceReference;
        if(renderedText.Any(c=>!char.IsControl(c)&&c>0x024f&&c is not (>= '\u2000' and <= '\u206f')))
            throw new NotSupportedException("This template currently supports Latin-script text. A reviewed script-specific font and layout is required for these records.");
        using var document=new PdfDocument();document.Info.Title=certificate.Kind=="Transcript"?"Academic transcript":"Course completion record";document.Info.Author="VHEI ERP";
        var font=new XFont("VHEI Noto Sans",10);var small=new XFont("VHEI Noto Sans",8);var title=new XFont("VHEI Noto Sans",19);
        XGraphics graphics=null!;double y=0;var pageNumber=0;
        void Page()
        {
            graphics?.Dispose();var page=document.AddPage();page.Size=PdfSharp.PageSize.A4;graphics=XGraphics.FromPdfPage(page);pageNumber++;
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(18,61,55)),0,0,page.Width.Point,14);
            graphics.DrawString(certificate.IsDemo?"DEMONSTRATION - NOT AN OFFICIAL AWARD":"VHEI ERP / Academic document",small,XBrushes.DarkSlateGray,new XPoint(48,40));
            graphics.DrawString($"Template AC-1 | {certificate.Number} | Page {pageNumber}",small,XBrushes.Gray,new XPoint(48,808));y=70;
        }
        void Line(string text,XFont? selected=null)
        {
            selected??=font;if(y>750)Page();graphics.DrawString(text,selected,XBrushes.Black,new XPoint(48,y));y+=selected.Size+7;
        }
        void Paragraph(string text,XFont? selected=null)
        {
            selected??=font;var line="";
            foreach(var word in text.Replace('\r',' ').Replace('\n',' ').Split(' ',StringSplitOptions.RemoveEmptyEntries))
            {
                if(graphics.MeasureString(line.Length==0?word:line+" "+word,selected).Width<=495){line=line.Length==0?word:line+" "+word;continue;}
                if(line.Length>0){Line(line,selected);line="";}
                foreach(var ch in word){if(graphics.MeasureString(line+ch,selected).Width>495){Line(line,selected);line="";}line+=ch;}
            }
            if(line.Length>0)Line(line,selected);
        }
        try
        {
            Page();Paragraph(root.GetProperty("institution").GetString()!,title);y+=8;
            Paragraph(certificate.Kind=="Transcript"?"Academic transcript":"Course completion record",title);y+=8;
            Paragraph("Student: "+root.GetProperty("student").GetString());Paragraph("Student number: "+root.GetProperty("studentNumber").GetString());
            Paragraph("Document: "+certificate.Number,small);Paragraph("Issued (UTC): "+certificate.IssuedAt?.ToString("yyyy-MM-dd HH:mm"),small);y+=12;
            if(certificate.Kind=="CourseCompletion")
            {
                Paragraph("The recorded published results meet the independently approved curriculum policy identified below. Institutional clearance evidence was reviewed at issuance.");
                Paragraph("Curriculum policy: "+certificate.GraduationPolicyId,small);Paragraph("Clearance reference: "+certificate.ClearanceReference,small);y+=12;
            }
            var results=root.GetProperty("results");
            foreach(var row in results.GetProperty("results").EnumerateArray())
            {
                if(y>655)Page();graphics.DrawLine(XPens.LightGray,48,y,547,y);y+=17;
                Paragraph(row.GetProperty("Code").GetString()+" | "+row.GetProperty("subject").GetString());
                Paragraph(row.GetProperty("programme").GetString()+" | "+row.GetProperty("session").GetString()+" | Semester "+row.GetProperty("Semester"),small);
                Paragraph($"Credits {row.GetProperty("Credits")} | Total {row.GetProperty("Total")} | {row.GetProperty("Percentage")}% | Grade {row.GetProperty("Grade")} | Points {row.GetProperty("GradePoints")} | {(row.GetProperty("IsPass").GetBoolean()?"Pass":"Backlog")}",small);y+=5;
            }
            y+=10;foreach(var item in results.GetProperty("semesters").EnumerateArray())Paragraph($"{item.GetProperty("programme")} / {item.GetProperty("session")} / Semester {item.GetProperty("Semester")}: recorded SGPA {item.GetProperty("sgpa")}; credits {item.GetProperty("credits")}; backlogs {item.GetProperty("backlogs")}",small);
            foreach(var item in results.GetProperty("cumulative").EnumerateArray())Paragraph($"{item.GetProperty("programme")}: recorded CGPA {item.GetProperty("cgpa")}; credits {item.GetProperty("credits")}; backlogs {item.GetProperty("backlogs")}",small);
            y+=10;Paragraph(results.GetProperty("note").GetString()!,small);Paragraph("Authority reference: "+certificate.AuthorityReference,small);
            Paragraph("Verify the document status and compare its SHA-256 with the verification record. This PDF has no cryptographic digital signature.",small);
            if(y>610)Page();y+=10;
            using var generator=new QRCodeGenerator();using var data=generator.CreateQrCode(verificationUrl,QRCodeGenerator.ECCLevel.Q);
            var module=100d/data.ModuleMatrix.Count;
            graphics.DrawRectangle(XBrushes.White,48,y,100,100);
            for(var row=0;row<data.ModuleMatrix.Count;row++)for(var column=0;column<data.ModuleMatrix.Count;column++)if(data.ModuleMatrix[row][column])graphics.DrawRectangle(XBrushes.Black,48+column*module,y+row*module,module,module);
            y+=118;Paragraph(verificationUrl,small);
            graphics.Dispose();using var output=new MemoryStream();document.Save(output);return output.ToArray();
        }
        finally{graphics?.Dispose();}
    }
    private sealed class EmbeddedFont:IFontResolver
    {
        private static readonly byte[] Font=Read();
        private static byte[] Read(){using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("VHEI.Api.Assets.NotoSans-Regular.ttf")??throw new InvalidOperationException("Embedded font missing.");using var memory=new MemoryStream();stream.CopyTo(memory);return memory.ToArray();}
        public byte[] GetFont(string faceName)=>Font;
        public FontResolverInfo ResolveTypeface(string familyName,bool bold,bool italic)=>new("VHEINotoSans");
    }
}
