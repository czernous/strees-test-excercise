import { Suspense, useState } from 'react';
import ServerHealthCheck from './components/ServerHealthCheck';
import CalculationForm from './components/CalculationForm';
import CalculationList from './components/CalculationList';
import CalculationDetails from './components/CalculationDetails';
import { ErrorBoundary } from './components/ErrorBoundary';
import type { CreateCalculationResponse } from './types';

function LoadingSpinner() {
    return (
        <div className="flex justify-center items-center p-12">
            <div className="flex flex-col items-center gap-4">
                <span className="loading loading-spinner loading-lg"></span>
                <p className="text-sm text-base-content/70">Loading data from server...</p>
            </div>
        </div>
    );
}

function ServerLoadingScreen() {
    return (
        <div className="min-h-screen flex items-center justify-center bg-base-200">
            <div className="card bg-base-100 shadow-xl">
                <div className="card-body items-center text-center">
                    <div className="loading loading-spinner loading-lg text-primary"></div>
                    <h2 className="card-title text-2xl mt-4">Waiting for server...</h2>
                    <p className="text-base-content/70">
                        Attempting to connect to the backend server
                    </p>
                    <div className="text-sm text-base-content/50 mt-2">
                        Make sure the backend server is running on https://localhost:7044
                    </div>
                </div>
            </div>
        </div>
    );
}

function App() {
    const [selectedCalculationId, setSelectedCalculationId] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [refreshKey, setRefreshKey] = useState(0);

    const handleCalculationCreated = (calculation: CreateCalculationResponse) => {
        setSuccessMessage(
            `Calculation completed successfully in ${calculation.durationMs}ms. Total Expected Loss: ${calculation.totalExpectedLoss.toFixed(2)}`
        );
        
        // Trigger refresh of the list by changing the key
        setRefreshKey(prev => prev + 1);
        
        // Clear success message after 5 seconds
        setTimeout(() => setSuccessMessage(null), 5000);
    };

    const handleSelectCalculation = (id: string) => {
        setSelectedCalculationId(id);
    };

    const handleCloseDetails = () => {
        setSelectedCalculationId(null);
    };

    return (
        <Suspense fallback={<ServerLoadingScreen />}>
            <ServerHealthCheck>
                <div className="min-h-screen bg-base-200">
                {/* Header */}
                <header className="navbar bg-primary text-primary-content shadow-lg">
                <div className="container mx-auto">
                    <div className="flex-1">
                        <h1 className="text-3xl font-bold">Stress Test Application</h1>
                    </div>
                    <div className="flex-none">
                        <div className="text-sm opacity-80">
                            Portfolio Risk Assessment & House Price Scenario Analysis
                        </div>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <main className="container mx-auto p-6 space-y-6">
                {/* Success Message */}
                {successMessage && (
                    <div className="alert alert-success shadow-lg animate-pulse">
                        <div>
                            <svg xmlns="http://www.w3.org/2000/svg" className="stroke-current flex-shrink-0 h-6 w-6" fill="none" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            <span>{successMessage}</span>
                        </div>
                    </div>
                )}

                {selectedCalculationId ? (
                    <ErrorBoundary>
                        <Suspense fallback={<LoadingSpinner />}>
                            <CalculationDetails
                                calculationId={selectedCalculationId}
                                onClose={handleCloseDetails}
                            />
                        </Suspense>
                    </ErrorBoundary>
                ) : (
                    <>
                        <section>
                            <ErrorBoundary>
                                <Suspense fallback={<LoadingSpinner />}>
                                    <CalculationForm onCalculationCreated={handleCalculationCreated} />
                                </Suspense>
                            </ErrorBoundary>
                        </section>

                        <section>
                            <ErrorBoundary>
                                <Suspense key={refreshKey} fallback={<LoadingSpinner />}>
                                    <CalculationList onSelectCalculation={handleSelectCalculation} />
                                </Suspense>
                            </ErrorBoundary>
                        </section>
                    </>
                )}
            </main>

            {/* Footer */}
            <footer className="footer footer-center p-4 bg-base-300 text-base-content mt-12">
                <div>
                    <p>Stress Test Application © 2026</p>
                </div>
            </footer>
        </div>
            </ServerHealthCheck>
        </Suspense>
    );
}

export default App;
