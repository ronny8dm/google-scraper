import { useLocation, useNavigate, Link } from 'react-router-dom'
import { useState, useEffect } from 'react'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { BusinessTable } from '@/components/BusinessTable'

interface BusinessListing {
  name: string
  address?: string
  phone?: string
  website?: string
  rating?: number
  reviewCount?: number
  category?: string
  hours?: string
  googleMapsUrl?: string
  description?: string
}

interface ScrapingResult {
  businesses: BusinessListing[]
  totalFound: number
  success: boolean
  errorMessage?: string
  query: string
  scrapedAt: string
}

function Results() {
  const location = useLocation()
  const navigate = useNavigate()
  const [results, setResults] = useState<ScrapingResult | null>(null)

  useEffect(() => {
    const state = location.state as {
      results: ScrapingResult
      query: string
      maxResults: number
    }

    if (!state?.results) {
      navigate('/scraper')
      return
    }

    setResults(state.results)
  }, [location.state, navigate])

  const exportToCsv = () => {
    if (!results?.businesses) return
    
    const headers = [
      'Name', 'Category', 'Rating', 'Reviews', 'Address', 
      'Phone', 'Website', 'Hours', 'Google Maps URL', 'Description'
    ]
    
    const csvContent = [
      headers.join(','),
      ...results.businesses.map(business => [
        `"${(business.name || '').replace(/"/g, '""')}"`,
        `"${(business.category || '').replace(/"/g, '""')}"`,
        business.rating || '',
        business.reviewCount || '',
        `"${(business.address || '').replace(/"/g, '""')}"`,
        `"${(business.phone || '').replace(/"/g, '""')}"`,
        `"${(business.website || '').replace(/"/g, '""')}"`,
        `"${(business.hours || '').replace(/"/g, '""')}"`,
        `"${(business.googleMapsUrl || '').replace(/"/g, '""')}"`,
        `"${(business.description || '').replace(/"/g, '""')}"`
      ].join(','))
    ].join('\n')

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${results.query.replace(/\s+/g, '_')}_businesses_${new Date().toISOString().slice(0, 10)}.csv`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    window.URL.revokeObjectURL(url)
  }

  if (!results) {
    return (
      <div className="container mx-auto py-8 px-4">
        <div className="text-center">
          <p>Loading results...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="container mx-auto py-8 px-4">
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold">Scraping Results</h1>
            <div className="flex items-center gap-2 text-muted-foreground mt-1">
              <span>Query: <span className="font-medium">"{results.query}"</span></span>
              <span>•</span>
              <span>Found: <span className="font-medium">{results.totalFound} businesses</span></span>
              <span>•</span>
              <span>Scraped: <span className="font-medium">{new Date(results.scrapedAt).toLocaleString()}</span></span>
            </div>
          </div>
          <div className="flex gap-2">
            <Link to="/scraper">
              <Button variant="outline">
                <ArrowLeft className="w-4 h-4 mr-2" />
                New Search
              </Button>
            </Link>
          </div>
        </div>

        {/* Business Table */}
        <BusinessTable 
          businesses={results.businesses}
          query={results.query}
          onExportCsv={exportToCsv}
        />
      </div>
    </div>
  )
}

export default Results