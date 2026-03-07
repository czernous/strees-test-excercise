import { useState } from 'react';
import { createCalculation, invalidateCache } from '../../services/api';
import type { CreateCalculationResponse } from '../../types';

// Default values as per exercise example
const DEFAULT_VALUES: Record<string, string> = {
    'GB': '-5.12',
    'US': '-4.34',
    'FR': '-3.87',
    'DE': '-1.23',
    'SG': '-5.5',
    'GR': '-5.68'
};

export function initializeFormValues(countries: string[]): Record<string, string> {
    const initialValues: Record<string, string> = {};
    for (const country of countries) {
        initialValues[country] = DEFAULT_VALUES[country] || '0';
    }
    return initialValues;
}

export function parseFormData(formData: FormData): Record<string, number> {
    const housePriceChanges: Record<string, number> = {};

    for (const [key, value] of formData.entries()) {
        if (key.startsWith('country-')) {
            const country = key.replace('country-', '');
            const numValue = Number.parseFloat(value as string);
            if (Number.isNaN(numValue)) {
                throw new TypeError(`Invalid value for ${country}`);
            }
            housePriceChanges[country] = numValue;
        }
    }

    return housePriceChanges;
}

export function useCalculationFormController(
    onCalculationCreated: (calculation: CreateCalculationResponse) => void
) {
    const [error, setError] = useState<string | null>(null);
    const [isPending, setIsPending] = useState(false);

    const handleSubmit = async (formData: FormData) => {
        setIsPending(true);
        setError(null);

        try {
            const housePriceChanges = parseFormData(formData);
            const result = await createCalculation({ housePriceChanges });
            invalidateCache('calculations');
            onCalculationCreated(result);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to create calculation');
        } finally {
            setIsPending(false);
        }
    };

    return {
        error,
        isPending,
        handleSubmit
    };
}
