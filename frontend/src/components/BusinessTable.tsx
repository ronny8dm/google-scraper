import { useState } from 'react'
import { Download, MapPin, Phone, Globe, Star, ExternalLink, ChevronDown, ChevronUp } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'

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

interface BusinessTableProps {
  businesses: BusinessListing[]
  query: string
  onExportCsv?: () => void
}

export function BusinessTable({ businesses, onExportCsv }: BusinessTableProps) {
  const [expandedRows, setExpandedRows] = useState<Set<number>>(new Set())

  const toggleRow = (index: number) => {
    const newExpanded = new Set(expandedRows)
    if (newExpanded.has(index)) {
      newExpanded.delete(index)
    } else {
      newExpanded.add(index)
    }
    setExpandedRows(newExpanded)
  }

  const businessesWithRatings = businesses.filter(b => b.rating && b.rating > 0)
  const businessesWithPhones = businesses.filter(b => b.phone)
  const businessesWithWebsites = businesses.filter(b => b.website)
  const averageRating = businessesWithRatings.length > 0 
    ? businessesWithRatings.reduce((sum, b) => sum + (b.rating || 0), 0) / businessesWithRatings.length 
    : 0

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4 text-center">
            <div className="text-2xl font-bold text-blue-600">{businesses.length}</div>
            <div className="text-sm text-muted-foreground">Total Found</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 text-center">
            <div className="text-2xl font-bold text-green-600">{businessesWithRatings.length}</div>
            <div className="text-sm text-muted-foreground">With Ratings</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 text-center">
            <div className="text-2xl font-bold text-purple-600">{businessesWithPhones.length}</div>
            <div className="text-sm text-muted-foreground">With Phones</div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 text-center">
            <div className="text-2xl font-bold text-orange-600">{businessesWithWebsites.length}</div>
            <div className="text-sm text-muted-foreground">With Websites</div>
          </CardContent>
        </Card>
      </div>

      {/* Average Rating Card */}
      {averageRating > 0 && (
        <Card>
          <CardContent className="p-4 text-center">
            <div className="text-lg">
              Average Rating: <span className="font-bold">{averageRating.toFixed(1)}</span>
              <div className="inline-flex items-center ml-2">
                {[1, 2, 3, 4, 5].map(i => (
                  <Star 
                    key={i} 
                    className={`w-4 h-4 ${i <= averageRating ? 'fill-yellow-400 text-yellow-400' : 'text-gray-300'}`} 
                  />
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Business Table */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Business Listings</CardTitle>
          {onExportCsv && (
            <Button onClick={onExportCsv} className="bg-green-600 hover:bg-green-700">
              <Download className="w-4 h-4 mr-2" />
              Export CSV
            </Button>
          )}
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[50px]"></TableHead>
                  <TableHead>Business Name</TableHead>
                  <TableHead>Category</TableHead>
                  <TableHead>Rating</TableHead>
                  <TableHead>Contact</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {businesses.map((business, index) => (
                  <>
                    <TableRow key={index} className="hover:bg-muted/50">
                      <TableCell>
                        <Collapsible>
                          <CollapsibleTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => toggleRow(index)}
                              className="p-0 h-8 w-8"
                            >
                              {expandedRows.has(index) ? (
                                <ChevronUp className="h-4 w-4" />
                              ) : (
                                <ChevronDown className="h-4 w-4" />
                              )}
                            </Button>
                          </CollapsibleTrigger>
                        </Collapsible>
                      </TableCell>
                      <TableCell>
                        <div>
                          <div className="font-medium text-base">{business.name}</div>
                          {business.address && (
                            <div className="flex items-start gap-1 text-sm text-muted-foreground mt-1">
                              <MapPin className="w-3 h-3 mt-0.5 flex-shrink-0" />
                              <span className="line-clamp-1">{business.address}</span>
                            </div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        {business.category && (
                          <Badge variant="secondary" className="text-xs">
                            {business.category}
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell>
                        {business.rating && business.rating > 0 ? (
                          <div className="flex items-center gap-1">
                            <Star className="w-4 h-4 fill-yellow-400 text-yellow-400" />
                            <span className="font-medium">{business.rating.toFixed(1)}</span>
                            {business.reviewCount && (
                              <span className="text-sm text-muted-foreground">
                                ({business.reviewCount})
                              </span>
                            )}
                          </div>
                        ) : (
                          <span className="text-muted-foreground text-sm">No rating</span>
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col gap-1">
                          {business.phone && (
                            <div className="flex items-center gap-1 text-sm">
                              <Phone className="w-3 h-3" />
                              <a href={`tel:${business.phone}`} className="hover:underline text-blue-600">
                                {business.phone}
                              </a>
                            </div>
                          )}
                          {business.website && (
                            <div className="flex items-center gap-1 text-sm">
                              <Globe className="w-3 h-3" />
                              <a 
                                href={business.website} 
                                target="_blank" 
                                rel="noopener noreferrer" 
                                className="hover:underline text-blue-600"
                              >
                                Website
                              </a>
                            </div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          {business.googleMapsUrl && (
                            <a
                              href={business.googleMapsUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                            >
                              <Button variant="outline" size="sm">
                                <ExternalLink className="w-3 h-3 mr-1" />
                                Maps
                              </Button>
                            </a>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                    
                    {/* Expanded Row Content */}
                    {expandedRows.has(index) && (
                      <TableRow>
                        <TableCell></TableCell>
                        <TableCell colSpan={5}>
                          <Collapsible open={expandedRows.has(index)}>
                            <CollapsibleContent>
                              <div className="p-4 bg-muted/30 rounded-lg space-y-3">
                                {business.hours && (
                                  <div>
                                    <h4 className="font-medium text-sm mb-1">Hours:</h4>
                                    <p className="text-sm text-muted-foreground">{business.hours}</p>
                                  </div>
                                )}
                                {business.description && (
                                  <div>
                                    <h4 className="font-medium text-sm mb-1">Description:</h4>
                                    <p className="text-sm text-muted-foreground">{business.description}</p>
                                  </div>
                                )}
                                {business.address && (
                                  <div>
                                    <h4 className="font-medium text-sm mb-1">Full Address:</h4>
                                    <p className="text-sm text-muted-foreground">{business.address}</p>
                                  </div>
                                )}
                              </div>
                            </CollapsibleContent>
                          </Collapsible>
                        </TableCell>
                      </TableRow>
                    )}
                  </>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}