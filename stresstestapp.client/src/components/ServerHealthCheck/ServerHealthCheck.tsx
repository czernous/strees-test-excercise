import { use, type ReactNode } from 'react';
import { waitForServerHealth } from '../../services/api';
import { isServerHealthy } from './controller';

interface ServerHealthCheckProps {
    readonly children: ReactNode;
}

export default function ServerHealthCheck({ children }: ServerHealthCheckProps) {
    const healthCheckResult = use(waitForServerHealth());

    if (!isServerHealthy(healthCheckResult)) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-base-200">
                <div className="card bg-base-100 shadow-xl">
                    <div className="card-body items-center text-center">
                        <svg xmlns="http://www.w3.org/2000/svg" className="stroke-current flex-shrink-0 h-12 w-12 text-error" fill="none" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <h2 className="card-title text-2xl mt-4 text-error">Server Unavailable</h2>
                        <p className="text-base-content/70">
                            Could not connect to the backend server after multiple attempts.
                        </p>
                        <div className="alert alert-warning mt-4">
                            <div>
                                <span className="text-sm">
                                    Please start the backend server and refresh this page.
                                    <br />
                                    <code className="text-xs">dotnet run --project StressTestApp.Server</code>
                                </span>
                            </div>
                        </div>
                        <div className="card-actions mt-4">
                            <button onClick={() => globalThis.location.reload()} className="btn btn-primary">
                                Retry
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    return <>{children}</>;
}
