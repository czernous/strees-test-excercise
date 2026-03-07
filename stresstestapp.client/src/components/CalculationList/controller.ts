import type { CalculationSummary } from '../../types';

export function hasCalculations(calculations: CalculationSummary[]): boolean {
    return calculations.length > 0;
}

export function getCalculationCount(calculations: CalculationSummary[]): number {
    return calculations.length;
}
