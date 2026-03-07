import { use, useState } from 'react';
import { getCountries } from '../../services/api';
import { useCalculationFormController, initializeFormValues } from './controller';
import type { CreateCalculationResponse } from '../../types';

interface CalculationFormProps {
    readonly onCalculationCreated: (calculation: CreateCalculationResponse) => void;
}

export default function CalculationForm({ onCalculationCreated }: CalculationFormProps) {
    const { countries } = use(getCountries());
    const { error, isPending, handleSubmit } = useCalculationFormController(onCalculationCreated);
    
    const [values, setValues] = useState<Record<string, string>>(() => 
        initializeFormValues(countries)
    );

    const handleChange = (country: string, value: string) => {
        setValues(prev => ({ ...prev, [country]: value }));
    };

    const onSubmit = async (e: React.SyntheticEvent<HTMLFormElement>) => {
        e.preventDefault();
        const formData = new FormData(e.currentTarget);
        await handleSubmit(formData);
    };

    return (
        <div className="card bg-base-100 shadow-xl">
            <div className="card-body">
                <h2 className="card-title text-3xl">Create New Stress Test Calculation</h2>
                <p className="text-base-content/70 mb-4">
                    Specify the percentage change in house prices for each country. 
                    Negative values indicate a price decrease.
                </p>

                <form onSubmit={onSubmit}>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
                        {countries.map(country => (
                            <div key={country} className="form-control">
                                <label className="label" htmlFor={`country-${country}`}>
                                    <span className="label-text font-bold text-lg">{country}</span>
                                </label>
                                <label className="input-group">
                                    <input
                                        id={`country-${country}`}
                                        name={`country-${country}`}
                                        type="number"
                                        step="0.01"
                                        value={values[country] ?? '0'}
                                        onChange={(e) => handleChange(country, e.target.value)}
                                        className="input input-bordered w-full"
                                        required
                                    />
                                    <span className="bg-base-300">%</span>
                                </label>
                            </div>
                        ))}
                    </div>

                    {error && (
                        <div className="alert alert-error shadow-lg mb-4">
                            <div>
                                <svg xmlns="http://www.w3.org/2000/svg" className="stroke-current flex-shrink-0 h-6 w-6" fill="none" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
                                </svg>
                                <span>{error}</span>
                            </div>
                        </div>
                    )}

                    <div className="card-actions justify-end">
                        <button 
                            type="submit" 
                            className={`btn btn-primary btn-lg ${isPending ? 'loading' : ''}`}
                            disabled={isPending}
                        >
                            {isPending ? 'Calculating...' : 'Run Calculation'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
