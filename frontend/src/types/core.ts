export type Institution={id:string;code:string;name:string;address:string|null;website:string|null;sourceUrl:string;verificationStatus:string};
export type Me={id:string;email:string;displayName:string;roles:string[];institutions:Institution[];mfaEnabled:boolean};
export type Programme={id:string;name:string;durationYears:number|null;verificationStatus:string;sourceUrl:string};
export type Department={id:string;name:string;verificationStatus:string;sourceUrl:string};
export type Reference={institution:Institution;departments:Department[];programmes:Programme[];sessions:{id:string;name:string;isDemo:boolean}[]};
