import { use } from 'react';
import { getCalculations } from '../../services/api';
import { formatDate, formatCurrency, formatPercentage } from '../../utils/formatters';
import { hasCalculations, getCalculationCount } from './controller';

interface CalculationListProps {
    readonly onSelectCalculation: (id: string) => void;
}

export default function CalculationList({ onSelectCalculation }: CalculationListProps) {
    const { calculations } = use(getCalculations());

    if (!hasCalculations(calculations)) {
        return (
            <div className="alert alert-info shadow-lg">
                <div>
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" className="stroke-current flex-shrink-0 w-6 h-6">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                    </svg>
                    <span>No calculations yet. Create your first stress test calculation above!</span>
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-4">
            <div className="flex justify-between items-center">
                <h2 className="text-2xl font-bold">Calculation History</h2>
                <div className="badge badge-primary badge-lg">
                    Total runs: {getCalculationCount(calculations)}
                </div>
            </div>

            <div className="overflow-x-auto shadow-xl rounded-lg">
                <table className="table table-zebra w-full">
                    <thead>
                        <tr>
                            <th>Date & Time</th>
                            <th>Duration</th>
                            <th>Portfolios</th>
                            <th>Loans</th>
                            <th>Total Expected Loss</th>
                            <th>Countries</th>
                            <th>Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {calculations.map(calc => (
                            <tr key={calc.id} className="hover">
                                <td className="font-mono text-sm">{formatDate(calc.createdAtUtc)}</td>
                                <td>
                                    <div className="badge badge-ghost">{calc.durationMs}ms</div>
                                </td>
                                <td className="text-center">{calc.portfolioCount}</td>
                                <td className="text-center">{calc.loanCount}</td>
                                <td className="text-right font-semibold tabular-nums">
                                    {formatCurrency(calc.totalExpectedLoss)}
                                </td>
                                <td>
                                    <div className="flex flex-wrap gap-1">
                                        {Object.entries(calc.housePriceChanges).map(([country, change]) => (
                                            <div 
                                                key={country} 
                                                className={`badge badge-sm ${change < 0 ? 'badge-error' : 'badge-success'}`}
                                            >
                                                {country}: {formatPercentage(change)}
                                            </div>
                                        ))}
                                    </div>
                                </td>
                                <td>
                                    <button 
                                        className="btn btn-primary btn-sm"
                                        onClick={() => onSelectCalculation(calc.id)}
                                    >
                                        View Details
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}
