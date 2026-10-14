import {defineConfig} from '@playwright/test';
export default defineConfig({testDir:'./tests',use:{channel:'chromium',baseURL:'http://127.0.0.1:5173',trace:'retain-on-failure'},reporter:'list',workers:1});
