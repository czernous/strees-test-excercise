// API Request/Response Types

export interface CreateCalculationRequest {
    housePriceChanges: Record<string, number>;
}

export interface CreateCalculationResponse {
    calculationId: string;
    createdAtUtc: string;
    durationMs: number;
    housePriceChanges: Record<string, number>;
    portfolioCount: number;
    loanCount: number;
    totalExpectedLoss: number;
}

export interface CalculationSummary {
    id: string;
    createdAtUtc: string;
    durationMs: number;
    portfolioCount: number;
    loanCount: number;
    totalExpectedLoss: number;
    housePriceChanges: Record<string, number>;
}

export interface ListCalculationsResponse {
    calculations: CalculationSummary[];
}

export interface PortfolioCalculationResult {
    portfolioId: string;
    portfolioName: string;
    country: string;
    currency: string;
    totalOutstandingAmount: number;
    totalCollateralValue: number;
    totalScenarioCollateralValue: number;
    totalExpectedLoss: number;
    loanCount: number;
}

export interface GetCalculationResponse {
    id: string;
    createdAtUtc: string;
    durationMs: number;
    portfolioCount: number;
    loanCount: number;
    totalExpectedLoss: number;
    housePriceChanges: Record<string, number>;
    results: PortfolioCalculationResult[];
}

export interface ListCountriesResponse {
    countries: string[];
}
