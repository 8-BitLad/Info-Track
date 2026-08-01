import { useState } from 'react'
import './App.css'

type Screen = 'home' | 'listings' | 'insights-loading' | 'insights' | 'searchByCity'


function App() {
    const [screen, setScreen] = useState<Screen>('home')

    function goHome() {
        setScreen('home')
    }
   

    return (
        <main className="app-shell">
            <section className="workspace" aria-labelledby="page-title">
                <header className="page-header">
                    <p className="eyebrow">InfoTrack / discovery</p>
                    <h1 id="page-title">
                        {screen === 'home'
                            ? 'Solicitor Discovery'
                            : screen === 'insights' || screen === 'insights-loading'
                                ? 'Insights'
                                : 'Location Crawler'}
                    </h1>
                    <p className="subtitle">
                        {screen === 'home' ? (
                            <>
                                Start Search solicitor listing.
                            </>
                        ) : screen === 'insights' || screen === 'insights-loading' ? (
                            <>
                                Browse previously scraped solicitor listings. Sort by <strong>postcode</strong> or <strong>place name</strong> to surface nearby firms first.
                            </>
                        ) : screen === 'listings' ? (
                            <>
                                Browse solicitor cards extracted from the <strong>selected location</strong> page.
                            </>
                        ) : (
                            <>
                                Search persisted crawl results and open a location for <strong>card-based solicitor details</strong>.
                            </>
                        )}
                    </p>
                </header>

                {/* ── Home ── */}
                {screen === 'home' && (
                    <div className="home-screen">
                        <div className="mode-cards">
                            <button className="mode-card mode-card--search" type="button">
                                <span className="mode-card-accent" />
                                <span className="mode-card-title">Search By City</span>
                                <span className="mode-card-desc">
                                    Search solicitors by city.
                                </span>
                            </button>
                            <button className="mode-card mode-card--insights" type="button">
                                <span className="mode-card-accent" />
                                <span className="mode-card-title">Insights</span>
                                <span className="mode-card-desc">
                                    View scraped solicitor listings only. Sort results by postcode or
                                    place name to surface nearby firms first.
                                </span>
                            </button>
                        </div>
                    </div>
                )}

                {screen === 'searchByCity' && (
                    <>
                        <section className="crawl-controls" aria-label="Search by city">
                            <form onSubmit={(event) => {
                                event.preventDefault()                                
                            }}>
                                <label htmlFor="city-search">Search by city</label>
                                <div className="input-row">
                                    <input
                                        id="city-search"
                                        type="search"
                                        
                                        placeholder="Enter a city name"
                                    />
                                    <button className="primary-action" type="submit">
                                        Search
                                    </button>
                                </div>
                                <p className="input-hint">
                                    This search uses the configured city URL template from appsettings.
                                </p>
                                
                            </form>
                            <div className="secondary-search-actions">
                                <button className="quiet-action" type="button" onClick={() => goHome()}>
                                    &larr; Home
                                </button>
                            </div>
                        </section>

                        <section className="results-section" aria-labelledby="city-results-heading">
                            <div className="section-heading">
                                <h2 id="city-results-heading">City search results</h2>                               
                            </div>                        

                            
                        </section>
                    </>
                )}

                {screen === 'insights-loading' && (
                    <section className="results-section" aria-live="polite">
                        <div className="section-heading">
                            <h2>Loading searched listings</h2>
                            <span>Aggregating data...</span>
                        </div>
                        <div className="progress-wrap" aria-hidden="true">
                            <div className="progress-track">
                                <div className="progress-fill" style={{ width: `${insightsProgress}%` }} />
                            </div>
                            <p>Fetching solicitor listings across locations...</p>
                        </div>
                    </section>
                )}

                {/* ── Insights: results with sort ── */}
                {screen === 'insights' && (
                    <>
                        <section className="crawl-controls insights-controls" aria-label="Insights controls">
                            <form className="insights-form">
                                <label htmlFor="insight-sort">Search Nearest location</label>
                                <div className="input-row">
                                    <input
                                        id="insight-sort"
                                        type="search"                                        
                                        placeholder="Enter postcode"
                                        aria-describedby="postcode-hint"
                                    />
                                    <button className="primary-action" type="submit">
                                        Lookup
                                    </button>
                                </div>
                                
                            </form>
                            
                            <div className="secondary-actions">
                                <button className="quiet-action" type="button" onClick={() => goHome()}>
                                    &larr; Home
                                </button>
                            </div>
                        </section>

                                                
                    </>
                )}                
            </section>
        </main>
    )
}



export default App
