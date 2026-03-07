import { Component, type ReactNode } from 'react';

interface ErrorBoundaryProps {
    readonly children: ReactNode;
}

interface ErrorBoundaryState {
    error: Error | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
    state: ErrorBoundaryState = { error: null };

    static getDerivedStateFromError(error: Error): ErrorBoundaryState {
        return { error };
    }

    private readonly handleReset = () => {
        this.setState({ error: null });
    };

    render() {
        if (this.state.error) {
            return (
                <div className="alert alert-error shadow-lg max-w-2xl mx-auto my-8">
                    <div>
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            className="stroke-current flex-shrink-0 h-6 w-6"
                            fill="none"
                            viewBox="0 0 24 24"
                        >
                            <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth="2"
                                d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
                            />
                        </svg>
                        <div>
                            <h3 className="font-bold">Error Loading Data</h3>
                            <div className="text-sm">{this.state.error.message}</div>
                        </div>
                    </div>
                    <div className="flex-none">
                        <button className="btn btn-sm btn-ghost" onClick={this.handleReset}>
                            Retry
                        </button>
                    </div>
                </div>
            );
        }

        return this.props.children;
    }
}
