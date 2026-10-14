import type {ButtonHTMLAttributes} from 'react';
import {clsx} from 'clsx';
import {twMerge} from 'tailwind-merge';
export function Button({className,...props}:ButtonHTMLAttributes<HTMLButtonElement>){return <button className={twMerge(clsx('btn',className))} {...props}/>;}
