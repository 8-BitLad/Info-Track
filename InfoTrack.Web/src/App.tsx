import { useState } from 'react'
import './App.css'

type Screen = 'home' | 'startCrawl' | 'locations' | 'listings' | 'insights-loading' | 'insights' | 'searchByCity'

interface SolicitorCard {
    name: string
    phoneNumber: string | null
    email: string | null
    websiteUrl: string | null
    rating: number
    sourceUrl: string
    address: string | null
    reviewsCount: number
}

interface SolicitorListingsResponse {
    locationUrl: string
    source: string
    listings: SolicitorCard[]
}

function App() {
    const [screen, setScreen] = useState<Screen>('home')

    // Search Results state

    const [citySearchTerm, setCitySearchTerm] = useState('')
    const [cityCards, setCityCards] = useState<SolicitorCard[]>([])
    const [citySource, setCitySource] = useState<string | null>(null)
    const [isLoadingCityCards, setIsLoadingCityCards] = useState(false)
    const [cityCardsProgress, setCityCardsProgress] = useState(10)
    const [cityFormError, setCityFormError] = useState<string | null>(null)

    const [errorMessage, setErrorMessage] = useState<string | null>(null)

    async function startSearchByCity() {
        setErrorMessage(null)
        setCityFormError(null)
        setCitySearchTerm('')
        setCityCards([])
        setCitySource(null)
        setCityCardsProgress(10)
        setScreen('searchByCity')
    }

    async function loadCityListings(city: string) {
        setErrorMessage(null)
        setCityFormError(null)
        setIsLoadingCityCards(true)
        setCityCardsProgress(12)

        const url = new URL('/api/locations/city', window.location.origin)
        url.searchParams.append('city', city)
        url.searchParams.append('refresh', 'false')

        const response = await fetch(url.toString())
        if (!response.ok) {
            setErrorMessage('Unable to load solicitor listings for this city.')
            setCityCardsProgress(100)
            setIsLoadingCityCards(false)
            return
        }

        const result = (await response.json()) as SolicitorListingsResponse
        setCitySource(result.source)
        setCityCards(sortCards(result.listings))
        setCityCardsProgress(100)
        setIsLoadingCityCards(false)
    }

    function goHome() {
        setCitySearchTerm("");
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
                            <button className="mode-card mode-card--search" type="button" onClick={() => void startSearchByCity()}>
                                <span className="mode-card-accent" />
                                <span className="mode-card-title">Search By City</span>
                                <span className="mode-card-desc">
                                    Search solicitors by city.
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
                                if (!citySearchTerm.trim()) {
                                    setCityFormError('Please enter a city name.')
                                    return
                                }

                                void loadCityListings(citySearchTerm.trim())
                            }}>
                                <label htmlFor="city-search">Search by city</label>
                                <div className="input-row">
                                    <input
                                        id="city-search"
                                        type="search"
                                        value={citySearchTerm}
                                        onChange={(event) => {
                                            setCitySearchTerm(event.target.value)
                                            setCityFormError(null)
                                            setErrorMessage(null)
                                        }}
                                        placeholder="Enter a city name"
                                    />
                                    <button className="primary-action" type="submit">
                                        Search
                                    </button>
                                </div>
                                <p className="input-hint">
                                    This search uses the configured city URL template from appsettings.
                                </p>
                                {cityFormError && <p className="error-message" role="alert">{cityFormError}</p>}
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
                                <span>{cityCards.length} cards</span>
                            </div>
                            {isLoadingCityCards && (
                                <div className="progress-wrap cards-progress" aria-hidden="true">
                                    <div className="progress-track">
                                        <div className="progress-fill" style={{ width: `${cityCardsProgress}%` }} />
                                    </div>
                                    <p>Fetching solicitor listings for {citySearchTerm.trim()}...</p>
                                </div>
                            )}
                            {citySource && <p className="cards-meta">Source: {citySource}</p>}

                            {!isLoadingCityCards && cityCards.length === 0 ? (
                                <p className="cards-empty">Enter a city and click Search to load solicitor listings.</p>
                            ) : (
                                <div className="card-grid">
                                    {cityCards.map((card, index) => (
                                        <article className="solicitor-card" key={`${card.name}-${index}`}>
                                            <header>
                                                <h3>{card.name}</h3>
                                                <p>{formatRating(card.rating)} ({card.reviewsCount})</p>
                                            </header>
                                            <dl>
                                                <div>
                                                    <dt>Phone</dt>
                                                    <dd>
                                                        {card.phoneNumber ? (
                                                            <a href={`tel:${card.phoneNumber}`}>{card.phoneNumber}</a>
                                                        ) : (
                                                            'Not listed'
                                                        )}
                                                    </dd>
                                                </div>
                                                <div>
                                                    <dt>Email</dt>
                                                    <dd>
                                                        {card.email ? <a href={`mailto:${card.email}`}>{card.email}</a> : 'Not listed'}
                                                    </dd>
                                                </div>
                                                <div>
                                                    <dt>Website</dt>
                                                    <dd>
                                                        {card.websiteUrl ? (
                                                            <a href={card.websiteUrl} target="_blank" rel="noreferrer">
                                                                {card.websiteUrl}
                                                            </a>
                                                        ) : (
                                                            'Not listed'
                                                        )}
                                                    </dd>
                                                </div>
                                                {card.address && (
                                                    <div>
                                                        <dt>Address</dt>
                                                        <dd>{card.address}</dd>
                                                    </div>
                                                )}
                                            </dl>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </section>
                    </>
                )}
                
                {errorMessage && <p className="error-message" role="alert">{errorMessage}</p>}
            </section>
        </main>
    )
}

function sortCards(cards: SolicitorCard[]) {
    return [...cards].sort((left, right) => left.name.localeCompare(right.name))
}

function formatRating(rating: number) {
    if (rating <= 0) {
        return 'No rating'
    }

    return `${'★'.repeat(rating)}`
}

export default App
