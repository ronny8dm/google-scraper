
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { MapPin, Search, Download, BarChart3, Clock } from "lucide-react";

export const HeroCards = () => {
  return (
    <div className="hidden lg:grid grid-cols-3 grid-rows-3 gap-4 relative w-[700px] h-[500px]">
      {/* Search Feature - Large Card */}
      <Card className="col-span-2 row-span-2 drop-shadow-xl shadow-black/10 dark:shadow-white/10">
        <CardHeader>
          <div className="flex flex-col  gap-">
            <div className="bg-blue-500/20 p-2  flex items-center rounded-lg">
              <Search className="w-6 h-6 mr-2 text-blue-600" />
              <CardTitle className="text-sm">Smart Search</CardTitle>
            </div>
            <div>
             
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <img className="w-full h-auto" src="/src/assets/google-maps-ui.png" alt="Smart Search" />
        </CardContent>
      </Card>

      {/* Location Pin */}
      <Card className="drop-shadow-xl h-40 shadow-black/10 dark:shadow-white/10">
        <CardHeader className="">
          <div className="bg-red-500/20 p-2 flex items-center rounded-lg ">
            <MapPin className="w-5 h-5 mr-2  text-red-600" />
            <CardTitle className="text-sm">Precise Locations</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            GPS coordinates and full addresses for every business.
          </p>
        </CardContent>
      </Card>

      {/* Export Feature */}
      <Card className="drop-shadow-xl shadow-black/10 dark:shadow-white/10">
        <CardHeader className="pb-3">
          <div className="bg-green-500/20 flex items-center p-2 rounded-lg ">
            <Download className="w-5 h-5 mr-2 text-green-600" />
            <CardTitle className="text-sm">Export Data</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            CSV, JSON, Excel formats ready for your CRM.
          </p>
        </CardContent>
      </Card>

      {/* Analytics */}
      <Card className="col-span-2 drop-shadow-xl shadow-black/10 dark:shadow-white/10">
        <CardHeader>
          <div className="flex flex-col ">
            <div className="bg-purple-500/20 flex items-center p-2 rounded-lg">
              <BarChart3 className="w-6 h-6 mr-2 text-purple-600" />
              <CardTitle className="text-sm">Business Analytics</CardTitle>
            </div>
            <div>
              
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-3 gap-4 text-center">
            <div>
              <p className="text-2xl font-bold text-blue-600">4.8★</p>
              <p className="text-xs text-muted-foreground">Avg Rating</p>
            </div>
            <div>
              <p className="text-2xl font-bold text-green-600">1.2k</p>
              <p className="text-xs text-muted-foreground">Reviews</p>
            </div>
            <div>
              <p className="text-2xl font-bold text-orange-600">24/7</p>
              <p className="text-xs text-muted-foreground">Hours</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Real-time */}
      <Card className="drop-shadow-xl shadow-black/10 dark:shadow-white/10">
        <CardHeader className="pb-3">
          <div className="bg-orange-500/20 flex items-center p-2 rounded-lg ">
            <Clock className="w-5 h-5 mr-2 text-orange-600" />
            <CardTitle className="text-sm">Real-time</CardTitle>
          </div>
          
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            Live data extraction from Google Maps.
          </p>
        </CardContent>
      </Card>
    </div>
  );
};
