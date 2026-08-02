import { useEffect, useState } from 'react'
import './App.css'

type Screen = 'home' | 'insights-loading' | 'insights' | 'searchByCity'


function isValidPostcode(postCode: string): boolean {
    if (!postCode || typeof postCode !== 'string') return false

    const trimmed = postCode.trim()

    // Must contain a space (UK format: inward + outward code)
    if (!trimmed.includes(' ')) return false

    // Remove spaces and check minimum length (6 chars minimum for UK postcode)
    const cleanedLength = trimmed.replace(/\s/g, '').length
    if (cleanedLength < 6) return false

    return true
}


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

interface InsightListing {
    solicitorName: string
    phoneNumber: string | null
    email: string | null
    websiteUrl: string | null
    rating: number
    sourceUrl: string
    locationName: string
    locationCounty: string | null
    locationUrl: string
    address: string | null
    reviewsCount: number
}

function App() {
    const [screen, setScreen] = useState<Screen>('home')

    // Search Results state

    // Additional UI filters
    const [selectedCounty, setSelectedCounty] = useState<string | null>(null)
    const [selectedCity, setSelectedCity] = useState<string | null>(null)
    const [selectedRating, setSelectedRating] = useState<number | null>(null)
    const [selectedReviewCount, setSelectedReviewCount] = useState<number | null>(null)

    // Insights state
    const [insightListings, setInsightListings] = useState<InsightListing[]>([])
    const [insightSortTerm, setInsightSortTerm] = useState('')
    const [insightSearchTerm, setInsightSearchTerm] = useState('')
    const [insightsProgress, setInsightsProgress] = useState(8)
    const [insightFormError, setInsightFormError] = useState<string | null>(null)

    const [citySearchTerm, setCitySearchTerm] = useState('')
    const [cityCards, setCityCards] = useState<SolicitorCard[]>([])
    const [citySource, setCitySource] = useState<string | null>(null)
    const [isLoadingCityCards, setIsLoadingCityCards] = useState(false)
    const [cityCardsProgress, setCityCardsProgress] = useState(10)
    const [cityFormError, setCityFormError] = useState<string | null>(null)

    const [errorMessage, setErrorMessage] = useState<string | null>(null)

    // Insights loading progress simulation
    useEffect(() => {
        if (screen !== 'insights-loading') return
        const timer = window.setInterval(() => {
            setInsightsProgress((current) => Math.min(current + 3, 92))
        }, 130)
        return () => { window.clearInterval(timer) }
    }, [screen])


    const sortedAndFilteredInsights = (() => {
        const searchValue = insightSearchTerm.trim().toLowerCase()

        let results = insightListings

        // Apply county/city filters if selected
        if (selectedCounty) {
            results = results.filter((listing) => (listing.locationCounty ?? 'Unassigned') === selectedCounty)
        }

        if (selectedCity) {
            results = results.filter((listing) => listing.locationName === selectedCity)
        }

        if (selectedRating) {
            results = results.filter((listing) => listing.rating === selectedRating)
        }


        if (selectedReviewCount != null && selectedReviewCount === 0) // 0 review
            results = results.filter((listing) => listing.reviewsCount <= selectedReviewCount)
        else if (selectedReviewCount != null && selectedReviewCount > 0)
            results = results.filter((listing) => listing.reviewsCount >= selectedReviewCount)


        if (searchValue) {
            results = results.filter((listing) =>
                listing.solicitorName.toLowerCase().includes(searchValue) ||
                (listing.phoneNumber ?? '').toLowerCase().includes(searchValue) ||
                (listing.email ?? '').toLowerCase().includes(searchValue) ||
                listing.locationName.toLowerCase().includes(searchValue) ||
                (listing.locationCounty ?? '').toLowerCase().includes(searchValue)
            )
        }

        return results
    })()

    const selectedCountiesAndCities = [...new Map(insightListings.map(item => [`${item.locationCounty}-${item.locationName}`,
    { locationCounty: item.locationCounty, locationName: item.locationName }])).values()];

    // 1. Get a sorted, unique list of counties from selected list
    const counties = Array.from(new Set(selectedCountiesAndCities.map(item => item.locationCounty))).sort();

    // 2. Get cities filtered by the selected county (or all cities if no county is selected)
    const citiesForSelectedCounty = selectedCounty
        ? Array.from(new Set(selectedCountiesAndCities.filter(item => item.locationCounty === selectedCounty).map(item => item.locationName))).sort()
        : Array.from(new Set(selectedCountiesAndCities.map(item => item.locationName))).sort();


    async function startInsights() {
        // Validate postcode if provided
        if (insightSortTerm.trim()) {
            if (!isValidPostcode(insightSortTerm)) {
                setInsightFormError('Please enter a complete postcode (e.g., "SW1A 1AA") or leave empty to load all listings.')
                return
            }
        }

        setInsightFormError(null)
        setErrorMessage(null)
        setInsightsProgress(8)
        setScreen('insights-loading')
        await loadInsightListings(insightSortTerm)
    }

    async function startSearchByCity() {
        setErrorMessage(null)
        setCityFormError(null)
        setCitySearchTerm('')
        setCityCards([])
        setCitySource(null)
        setCityCardsProgress(10)
        setScreen('searchByCity')
    }

    async function loadInsightListings(postCode?: string) {
        const url = new URL('/api/insights/listings', window.location.origin)
        if (postCode) {
            url.searchParams.append('postCode', postCode)
        }
        const response = await fetch(url.toString())
        if (!response.ok) {
            setErrorMessage('Unable to load insight listings.')
            setInsightsProgress(100)
            setScreen('insights')
            return
        }
        const listings = (await response.json()) as InsightListing[]
        setInsightListings(listings)
        setInsightsProgress(100)
        setScreen('insights')
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
        setInsightSortTerm("");
        setCitySearchTerm("");
        setInsightSearchTerm("");
        setSelectedRating(0);
        setSelectedReviewCount(-1);
        setSelectedCity(null);
        setSelectedCounty(null);
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
                                Search for Solicitors
                            </>
                        ) : screen === 'insights' || screen === 'insights-loading' ? (
                            <>
                                Browse previously scraped solicitor listings. Sort by <strong>postcode</strong> or <strong>place name</strong> to surface nearby firms first.
                            </>
                        ) : screen === 'searchByCity' ? (
                            <>
                                Search for solicitors in a specific city.
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
                            <button className="mode-card mode-card--insights" type="button" onClick={() => void startInsights()}>
                                <span className="mode-card-accent" />
                                <span className="mode-card-title">Insights</span>
                                <span className="mode-card-desc">
                                    View solicitor listings. Sort results by postcode or
                                    place name to surface nearby results.
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
                                <h2 id="city-results-heading">Results</h2>
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

                {screen === 'insights' && (
                    <>
                        <section className="crawl-controls insights-controls" aria-label="Insights controls">
                            <form onSubmit={(event) => { event.preventDefault(); void startInsights() }} className="insights-form">
                                <label htmlFor="insight-sort">Search Nearest location</label>
                                <div className="input-row">
                                    <input
                                        id="insight-sort"
                                        type="search"
                                        value={insightSortTerm}
                                        onChange={(event) => {
                                            setInsightSortTerm(event.target.value)
                                            setInsightFormError(null)
                                        }}
                                        placeholder="Enter postcode"
                                        aria-describedby="postcode-hint"
                                    />
                                    <button className="primary-action" type="submit">
                                        Lookup
                                    </button>
                                </div>
                                <p id="postcode-hint" className="input-hint">
                                    Enter a complete postcode (e.g., SW1A 1AA) or leave empty to load all listings. Results will be ordered by <strong>closest solicitors by distance</strong>
                                </p>
                                {insightFormError && <p className="error-message" role="alert">{insightFormError}</p>}
                            </form>



                            <form onSubmit={(event) => { event.preventDefault() }} className="insights-form">
                                <label htmlFor="insight-search">Filter results</label>
                                <div className="input-row">
                                    <input
                                        id="insight-search"
                                        type="search"
                                        value={insightSearchTerm}
                                        onChange={(event) => setInsightSearchTerm(event.target.value)}
                                        placeholder="Search by name, phone, email or location"
                                    />
                                </div>
                            </form>
                            <div className="secondary-actions">
                                <button className="quiet-action" type="button" onClick={() => goHome()}>
                                    &larr; Home
                                </button>
                            </div>
                        </section>

                        <section className="crawl-controls insights-controls" aria-label="Insights controls">
                            <div className="filter-row">
                                <div className="input-row">
                                    <label htmlFor="county-select">County</label>
                                    <select
                                        id="county-select"
                                        value={selectedCounty ?? ''}
                                        onChange={(e) => { setSelectedCounty(e.target.value || null); setSelectedCity(null); }}>
                                        <option value="">All counties</option>
                                        {counties.map((c) => (
                                            <option key={c} value={c}>{c}</option>
                                        ))}
                                    </select>
                                    <label htmlFor="city-select">City / Location</label>
                                    <select
                                        id="city-select"
                                        value={selectedCity ?? ''}
                                        onChange={(e) => setSelectedCity(e.target.value || null)}>
                                        <option value="">All locations</option>
                                        {citiesForSelectedCounty.map((city) => (
                                            <option key={city} value={city}>{city}</option>
                                        ))}
                                    </select>
                                    <label htmlFor="rating-select">Rating</label>
                                    <select
                                        id="rating-select"
                                        onChange={(e) => setSelectedRating(e.target.value ? parseInt(e.target.value) : null)}>
                                        <option value="">All Ratings</option>
                                        {Array.from({ length: 5 }, (_, i) => {
                                            const stars = '*'.repeat(i + 1);
                                            return (
                                                <option key={i} value={i + 1}>
                                                    {stars}
                                                </option>
                                            );
                                        })}
                                    </select>
                                    <label htmlFor="review-select">Reviews</label>
                                    <select
                                        id="review-select"
                                        onChange={(e) => setSelectedReviewCount(e.target.value ? parseInt(e.target.value) : null)}>
                                        <option value="-1">All Reviews</option>
                                        <option value="0">No Reviews</option>
                                        {Array.from({ length: 4 }, (_, i) => {
                                            const values = [1, 100, 500, 1000];
                                            const review = values[i];
                                            return (
                                                <option key={i} value={review}>
                                                    More Than {review}
                                                </option>
                                            );
                                        })}
                                    </select>
                                </div>

                                <div className="input-row">

                                </div>
                            </div>
                        </section>
                        <section className="results-section" aria-labelledby="insights-heading">
                            <div className="section-heading">
                                <h2 id="insights-heading">Previously scraped solicitor listings</h2>
                                <span>{sortedAndFilteredInsights.length} results</span>
                            </div>
                            {insightListings.length === 0 ? (
                                <p className="cards-empty">
                                    No data available yet. Use Search Results to browse and scrape some locations first. This page displays solicitor listings only from locations you have previously clicked and browsed.
                                </p>
                            ) : sortedAndFilteredInsights.length === 0 ? (
                                <p className="cards-empty">No listings matched your search.</p>
                            ) : (
                                <div className="card-grid">
                                    {sortedAndFilteredInsights.map((listing, index) => (
                                        <article className="solicitor-card" key={`${listing.solicitorName}-${listing.locationUrl}-${index}`}>
                                            <header>
                                                <h3>{listing.solicitorName}</h3>
                                                <p>{formatRating(listing.rating)} ({listing.reviewsCount})</p>
                                            </header>
                                            <p className="insight-location-tag">
                                                {listing.locationName}
                                                {listing.locationCounty ? `, ${listing.locationCounty}` : ''}
                                            </p>
                                            <dl>
                                                <div>
                                                    <dt>Phone</dt>
                                                    <dd>
                                                        {listing.phoneNumber ? (
                                                            <a href={`tel:${listing.phoneNumber}`}>{listing.phoneNumber}</a>
                                                        ) : 'Not listed'}
                                                    </dd>
                                                </div>
                                                <div>
                                                    <dt>Email</dt>
                                                    <dd>
                                                        {listing.email ? (
                                                            <a href={`mailto:${listing.email}`}>{listing.email}</a>
                                                        ) : 'Not listed'}
                                                    </dd>
                                                </div>
                                                <div>
                                                    <dt>Website</dt>
                                                    <dd>
                                                        {listing.websiteUrl ? (
                                                            <a href={listing.websiteUrl} target="_blank" rel="noreferrer">
                                                                {listing.websiteUrl}
                                                            </a>
                                                        ) : 'Not listed'}
                                                    </dd>
                                                </div>
                                                {listing.address && (
                                                    <div>
                                                        <dt>Address</dt>
                                                        <dd>{listing.address}</dd>
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

    return `${'*'.repeat(rating)}`
}

export default App
