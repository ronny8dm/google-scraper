"use client"

import type React from "react"
import axios, { AxiosError } from "axios"
import { useState } from "react"
import { MapPin, Search, Bot, Download, Shield, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Alert, AlertDescription } from "@/components/ui/alert"
import { useNavigate } from "react-router-dom"

const apiClient = axios.create({
  baseURL: 'https://scrapeapi.ronnyjdiaz.com/',
  timeout: 900000, // 15 minutes max timeout
  withCredentials: true, 
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
    'Origin': 'https://superscrape.ronnyjdiaz.com'
  }
});

// Add polling configuration calculation
const calculatePollingConfig = (resultCount: number) => {
  // Base time per result in seconds (adjust these values based on your scraping speed)
  const timePerResult = 3; // seconds per result
  const minPollingInterval = 3000; // minimum 3 seconds
  const maxPollingInterval = 10000; // maximum 10 seconds
  
  // Calculate total estimated time needed
  const estimatedTotalSeconds = resultCount * timePerResult;
  
  // Calculate polling interval - scales with result count but stays within bounds
  const pollingInterval = Math.min(
    maxPollingInterval,
    Math.max(minPollingInterval, Math.floor(resultCount * 100))
  );
  
  // Calculate max attempts based on estimated time
  const maxAttempts = Math.ceil((estimatedTotalSeconds * 1000) / pollingInterval) + 10; // Add buffer

  return {
    pollingInterval,
    maxAttempts,
    estimatedMinutes: Math.ceil(estimatedTotalSeconds / 60)
  };
};

export default function ScraperForm() {
  const [searchQuery, setSearchQuery] = useState("")
  const [maxResults, setMaxResults] = useState("20")
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [statusMessage, setStatusMessage] = useState<string | null>(null)
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!searchQuery.trim()) {
      setError("Please enter a search query");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const resultCount = parseInt(maxResults);
      const { pollingInterval, maxAttempts, estimatedMinutes } = calculatePollingConfig(resultCount);
      
      console.log("Starting scrape with:", { searchQuery, maxResults });
      
      const jobResponse = await apiClient.post('/api/scraperapi/scrape', {
        query: searchQuery,
        maxResults: resultCount
      });
      
      const jobId = jobResponse.data.jobId;
      let attempts = 0;
      
      setStatusMessage(
        `Job queued with ID: ${jobId}. ` +
        `Estimated time: ${estimatedMinutes} minutes`
      );
      
      const checkJobStatus = async () => {
        try {
          const statusResponse = await apiClient.get(`/api/scraperapi/job/${jobId}`);
          
          if (!statusResponse.data.success && statusResponse.data.errorMessage?.includes("still")) {
            attempts++;
            if (attempts < maxAttempts) {
              setTimeout(checkJobStatus, pollingInterval);
              const remainingAttempts = maxAttempts - attempts;
              const remainingMinutes = Math.ceil((remainingAttempts * pollingInterval) / 60000);
              
              setStatusMessage(
                `Job in progress (${attempts}/${maxAttempts})... ` +
                `Estimated ${remainingMinutes} minutes remaining.`
              );
            } else {
              setError(
                `Job ${jobId} is taking longer than expected. ` +
                `You can check results later at /results/${jobId}`
              );
              setLoading(false);
            }
          } else {
            console.log("Scraping completed:", statusResponse.data);
            setLoading(false);
            navigate('/results', { 
              state: { 
                results: statusResponse.data,
                query: searchQuery,
                maxResults: resultCount
              }
            });
          }
        } catch (err) {
          console.error('Error checking job status:', err);
          setError('Failed to check job status. Please try again later.');
          setLoading(false);
        }
      };
      
      setTimeout(checkJobStatus, 2000);
      
    } catch (err) {
      console.error('Scraping error:', err)
      
      if (axios.isAxiosError(err)) {
        const axiosError = err as AxiosError<{ error?: string; message?: string }>
        
        if (axiosError.response) {
          // Server responded with error status
          const errorMessage = axiosError.response.data?.error || 
                              axiosError.response.data?.message || 
                              `Server error: ${axiosError.response.status}`
          setError(errorMessage)
        } else if (axiosError.request) {
          // Request made but no response received
          setError("Unable to connect to the server. Please check if the API is running.")
        } else {
          // Something else happened
          setError(axiosError.message)
        }
      } else {
        setError('An unexpected error occurred during scraping')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen p-4">
      <div className="mx-auto max-w-4xl space-y-8">
        {/* Header */}
        <div className="text-center space-y-4">
          <div className="inline-flex items-center gap-2 bg-blue-500/20 px-4 py-2 rounded-full">
            <MapPin className="h-5 w-5 text-blue-600" />
            <span className="font-semibold dark:text-white">Google Maps Business Scraper</span>
          </div>
        </div>

        {/* Error Alert */}
        {error && (
          <Alert className="border-red-200 bg-red-50 dark:bg-red-950 dark:border-red-800">
            <AlertDescription className="text-red-800 dark:text-red-200">
              <strong>Error:</strong> {error}
            </AlertDescription>
          </Alert>
        )}

        {/* Status Message */}
        {statusMessage && (
          <Alert className="border-blue-200 bg-blue-50 dark:bg-blue-950 dark:border-blue-800">
            <AlertDescription className="text-blue-800 dark:text-blue-200">
              {statusMessage}
            </AlertDescription>
          </Alert>
        )}

        {/* Main Form Card */}
        <Card className="shadow-lg border-0 bg-muted/50 backdrop-blur-sm">
          <CardHeader className="space-y-4">
            <CardDescription className="text-base leading-relaxed">
              Enter a search query to scrape business listings from Google Maps. For example: "restaurants in London",
              "dentists near me", "coffee shops in New York".
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <form onSubmit={handleSubmit} className="space-y-6">
              {/* Search Query */}
              <div className="space-y-2">
                <Label htmlFor="search-query" className="text-sm font-medium">
                  Search Query
                </Label>
                <Input
                  id="search-query"
                  placeholder="e.g., restaurants in London"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="h-12 text-base"
                  disabled={loading}
                  required
                />
                <p className="text-sm text-muted-foreground">Enter what you want to search for on Google Maps</p>
              </div>

              {/* Maximum Results */}
              <div className="space-y-2">
                <Label htmlFor="max-results" className="text-sm font-medium">
                  Maximum Results
                </Label>
                <Select value={maxResults} onValueChange={setMaxResults} disabled={loading}>
                  <SelectTrigger className="h-12">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="10">10 results</SelectItem>
                    <SelectItem value="20">20 results</SelectItem>
                    <SelectItem value="50">50 results</SelectItem>
                    <SelectItem value="100">100 results</SelectItem>
                  </SelectContent>
                </Select>
                <p className="text-sm text-muted-foreground">More results will take longer to scrape</p>
              </div>

              {/* Submit Button */}
              <Button
                type="submit"
                className="w-full h-12 bg-blue-600 hover:bg-blue-700 text-white font-medium"
                size="lg"
                disabled={loading || !searchQuery.trim()}
              >
                {loading ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Scraping in progress...
                  </>
                ) : (
                  <>
                    <Search className="mr-2 h-4 w-4" />
                    Start Scraping
                  </>
                )}
              </Button>
            </form>
          </CardContent>
        </Card>

        {/* Loading Progress Indicator */}
        {loading && (
          <Card className="border-blue-200 bg-blue-50 dark:bg-blue-950 dark:border-blue-800">
            <CardContent className="p-6">
              <div className="text-center space-y-4">
                <div className="flex justify-center">
                  <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
                </div>
                <div>
                  <h3 className="text-lg font-semibold text-blue-800 dark:text-blue-200">
                    Extracting Business Data...
                  </h3>
                  <p className="text-sm text-blue-700 dark:text-blue-300 mt-1">
                    This may take a few moments depending on the number of results requested.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Feature Cards */}
        <div className="grid md:grid-cols-3 gap-6">
          <Card className="border-0 bg-muted/50 backdrop-blur-sm shadow-md hover:shadow-lg transition-shadow">
            <CardContent className="p-6 text-center space-y-4">
              <div className="inline-flex items-center justify-center w-12 h-12 bg-blue-500/20 rounded-lg">
                <Bot className="h-6 w-6 text-blue-600" />
              </div>
              <div className="space-y-2">
                <h3 className="font-semibold text-lg">Automated</h3>
                <p className="text-sm text-muted-foreground leading-relaxed">
                  Uses headless browser automation to extract data
                </p>
              </div>
            </CardContent>
          </Card>

          <Card className="border-0 bg-muted/50 backdrop-blur-sm shadow-md hover:shadow-lg transition-shadow">
            <CardContent className="p-6 text-center space-y-4">
              <div className="inline-flex items-center justify-center w-12 h-12 bg-green-500/20 rounded-lg">
                <Download className="h-6 w-6 text-green-600" />
              </div>
              <div className="space-y-2">
                <h3 className="font-semibold text-lg">Export Ready</h3>
                <p className="text-sm text-muted-foreground leading-relaxed">
                  Download results as CSV for further analysis
                </p>
              </div>
            </CardContent>
          </Card>

          <Card className="border-0 bg-muted/50 backdrop-blur-sm shadow-md hover:shadow-lg transition-shadow">
            <CardContent className="p-6 text-center space-y-4">
              <div className="inline-flex items-center justify-center w-12 h-12 bg-orange-500/20 rounded-lg">
                <Shield className="h-6 w-6 text-orange-600" />
              </div>
              <div className="space-y-2">
                <h3 className="font-semibold text-lg">Respectful</h3>
                <p className="text-sm text-muted-foreground leading-relaxed">
                  Includes delays and follows best practices
                </p>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Additional Features */}
        <div className="grid md:grid-cols-2 gap-6">
          <Card className="border-0 bg-muted/50 backdrop-blur-sm shadow-md">
            <CardContent className="p-6">
              <div className="flex items-start gap-4">
                <div className="inline-flex items-center justify-center w-10 h-10 bg-red-500/20 rounded-lg flex-shrink-0">
                  <MapPin className="h-5 w-5 text-red-600" />
                </div>
                <div className="space-y-2">
                  <h3 className="font-semibold">Precise Locations</h3>
                  <p className="text-sm text-muted-foreground">
                    GPS coordinates and full addresses for every business.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="border-0 bg-muted/50 backdrop-blur-sm shadow-md">
            <CardContent className="p-6">
              <div className="flex items-start gap-4">
                <div className="inline-flex items-center justify-center w-10 h-10 bg-green-500/20 rounded-lg flex-shrink-0">
                  <Download className="h-5 w-5 text-green-600" />
                </div>
                <div className="space-y-2">
                  <h3 className="font-semibold">Export Data</h3>
                  <p className="text-sm text-muted-foreground">CSV, JSON, Excel formats ready for your CRM.</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
