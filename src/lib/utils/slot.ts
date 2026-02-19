// src/lib/utils/slot.ts
import type { Slot } from '$lib/types/Slot';

export function sameSlot(a: Slot | null, b: Slot | null): boolean {
  return (
    a?.startTime === b?.startTime &&
    a?.date === b?.date &&
    a?.techId === b?.techId
  );
}