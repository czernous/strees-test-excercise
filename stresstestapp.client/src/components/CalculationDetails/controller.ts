import type { PortfolioCalculationResult } from '../../types';

export function calculateTotalOutstanding(results: PortfolioCalculationResult[]): number {
    return results.reduce((sum, r) => sum + r.totalOutstandingAmount, 0);
}

export function calculateTotalCollateral(results: PortfolioCalculationResult[]): number {
    return results.reduce((sum, r) => sum + r.totalCollateralValue, 0);
}

export function calculateTotalScenarioCollateral(results: PortfolioCalculationResult[]): number {
    return results.reduce((sum, r) => sum + r.totalScenarioCollateralValue, 0);
}

export function isNegativeChange(change: number): boolean {
    return change < 0;
}

export function getHousePriceChangeEntries(housePriceChanges: Record<string, number>): Array<[string, number]> {
    return Object.entries(housePriceChanges);
}

export function getBadgeColorClass(change: number): string {
    return isNegativeChange(change) ? 'bg-error text-error-content' : 'bg-success text-success-content';
}

