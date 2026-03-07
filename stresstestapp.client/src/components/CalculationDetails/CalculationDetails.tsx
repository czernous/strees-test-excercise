import { use } from 'react';
import { getCalculationById } from '../../services/api';
import { formatDate, formatCurrency, formatNumber, formatPercentage } from '../../utils/formatters';
import { 
    calculateTotalOutstanding, 
    calculateTotalCollateral, 
    calculateTotalScenarioCollateral,
    getHousePriceChangeEntries,
    getBadgeColorClass
} from './controller';

interface CalculationDetailsProps {
    readonly calculationId: string;
    readonly onClose: () => void;
}

export default function CalculationDetails({ calculationId, onClose }: CalculationDetailsProps) {
    const calculation = use(getCalculationById(calculationId));

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex justify-between items-center">
                <h2 className="text-3xl font-bold">Calculation Details</h2>
                <button 
                    className="btn btn-circle btn-ghost btn-lg"
                    onClick={onClose}
                    aria-label="Close"
                >
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>

            {/* Summary Card */}
            <div className="card bg-base-100 shadow-xl">
                <div className="card-body">
                    <h3 className="card-title text-2xl mb-4">Summary</h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        <div className="stat bg-base-200 rounded-lg">
                            <div className="stat-title">Calculation ID</div>
                            <div className="stat-value text-lg font-mono break-all">{calculation.id}</div>
                        </div>
                        <div className="stat bg-base-200 rounded-lg">
                            <div className="stat-title">Date & Time</div>
                            <div className="stat-value text-xl">{formatDate(calculation.createdAtUtc)}</div>
                        </div>
                        <div className="stat bg-base-200 rounded-lg">
                            <div className="stat-title">Duration</div>
                            <div className="stat-value text-2xl">{calculation.durationMs}<span className="text-lg">ms</span></div>
                        </div>
                        <div className="stat bg-base-200 rounded-lg">
                            <div className="stat-title">Portfolios</div>
                            <div className="stat-value text-3xl">{calculation.portfolioCount}</div>
                        </div>
                        <div className="stat bg-base-200 rounded-lg">
                            <div className="stat-title">Total Loans</div>
                            <div className="stat-value text-3xl">{formatNumber(calculation.loanCount)}</div>
                        </div>
                        <div className="stat bg-error text-error-content rounded-lg">
                            <div className="stat-title text-error-content/70">Total Expected Loss</div>
                            <div className="stat-value text-2xl">{formatCurrency(calculation.totalExpectedLoss)}</div>
                        </div>
                    </div>
                </div>
            </div>

            {/* House Price Changes */}
            <div className="card bg-base-100 shadow-xl">
                <div className="card-body">
                    <h3 className="card-title text-2xl mb-4">House Price Changes (Inputs)</h3>
                    <div className="flex flex-wrap gap-3">
                        {getHousePriceChangeEntries(calculation.housePriceChanges).map(([country, change]) => (
                            <div 
                                key={country} 
                                className={`stat ${getBadgeColorClass(change)} rounded-lg`}
                            >
                                <div className="stat-title text-current/70">{country}</div>
                                <div className="stat-value text-3xl">
                                    {formatPercentage(change)}
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Portfolio Results */}
            <div className="card bg-base-100 shadow-xl">
                <div className="card-body">
                    <h3 className="card-title text-2xl mb-4">Portfolio Results</h3>
                    <div className="overflow-x-auto">
                        <table className="table table-zebra w-full">
                            <thead>
                                <tr>
                                    <th>Portfolio ID</th>
                                    <th>Portfolio Name</th>
                                    <th>Country</th>
                                    <th>Currency</th>
                                    <th className="text-center">Loans</th>
                                    <th className="text-right">Outstanding</th>
                                    <th className="text-right">Collateral</th>
                                    <th className="text-right">Scenario Collateral</th>
                                    <th className="text-right">Expected Loss</th>
                                </tr>
                            </thead>
                            <tbody>
                                {calculation.results.map((result) => (
                                    <tr key={result.portfolioId} className="hover">
                                        <td className="font-mono text-sm">{result.portfolioId}</td>
                                        <td className="font-semibold">{result.portfolioName}</td>
                                        <td>
                                            <div className="badge badge-outline">{result.country}</div>
                                        </td>
                                        <td>{result.currency}</td>
                                        <td className="text-center tabular-nums">{result.loanCount}</td>
                                        <td className="text-right tabular-nums">{formatCurrency(result.totalOutstandingAmount)}</td>
                                        <td className="text-right tabular-nums">{formatCurrency(result.totalCollateralValue)}</td>
                                        <td className="text-right tabular-nums">{formatCurrency(result.totalScenarioCollateralValue)}</td>
                                        <td className="text-right font-bold tabular-nums text-error">{formatCurrency(result.totalExpectedLoss)}</td>
                                    </tr>
                                ))}
                            </tbody>
                            <tfoot>
                                <tr className="font-bold bg-base-300">
                                    <td colSpan={5}>TOTALS</td>
                                    <td className="text-right tabular-nums">
                                        {formatCurrency(calculateTotalOutstanding(calculation.results))}
                                    </td>
                                    <td className="text-right tabular-nums">
                                        {formatCurrency(calculateTotalCollateral(calculation.results))}
                                    </td>
                                    <td className="text-right tabular-nums">
                                        {formatCurrency(calculateTotalScenarioCollateral(calculation.results))}
                                    </td>
                                    <td className="text-right tabular-nums text-error text-lg">
                                        {formatCurrency(calculation.totalExpectedLoss)}
                                    </td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </div>
            </div>

            {/* Actions */}
            <div className="flex justify-end">
                <button className="btn btn-primary btn-lg" onClick={onClose}>
                    Close
                </button>
            </div>
        </div>
    );
}
