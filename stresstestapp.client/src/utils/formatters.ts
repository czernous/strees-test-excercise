export function formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleString();
}

export function formatCurrency(value: number, currency?: string): string {
    const formatted = new Intl.NumberFormat('en-US', {
        style: 'decimal',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(value);

    return currency ? `${formatted} ${currency}` : formatted;
}

export function formatNumber(value: number): string {
    return new Intl.NumberFormat('en-US').format(value);
}

export function formatPercentage(value: number): string {
    return `${value > 0 ? '+' : ''}${value}%`;
}
