# Embedded PDF font

NotoSans-Regular.ttf is the static hinted regular font from the official Noto fonts repository:
https://raw.githubusercontent.com/notofonts/noto-fonts/main/hinted/ttf/NotoSans/NotoSans-Regular.ttf

Retrieved 2026-10-20. SHA-256: b85c38ecea8a7cfb39c24e395a4007474fa5a4fc864f6ee33309eb4948d232d5
License: SIL Open Font License 1.1, copied from https://raw.githubusercontent.com/notofonts/noto-fonts/main/LICENSE into NotoSans-LICENSE.txt.

CertificatePdf uses an embedded custom font resolver so Windows/Linux deployment does not depend on installed system fonts. AC-1 currently accepts Latin-script content only and rejects unsupported scripts. Embedding this font alone does not establish Odia shaping or institutional template acceptance.