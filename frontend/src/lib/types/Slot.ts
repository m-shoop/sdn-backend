import type  { Service } from './Service.ts';

export type Slot = {
    startTime: string | null;
    date: string | null;
    techId: number | null;
    service: Service | null;
};