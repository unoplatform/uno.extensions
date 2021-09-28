

# Navigation Regions
Application is made up of a series of containers, such as a Frame, that support navigation. These are referred to as Regions

There are three types of containers that are currently supported, with others able to be supported by extending the BaseRegionManager class:  
**Frame** - Supports forward and backward navigation  
**TabView** - Supports forward navigation between tabs    
**ContentControl** - Supports forward navigation to show new content  


## Attributes
A region needs to be defined in XAML using one of these attached properties

**IsRegion = true/false**
This defines a region that doesn't have a name and the routing to this region is implicit. 

**RegionName = "region"**
This defines a region that has a name and where the name needs to be included in any navigation route.


## Navigation Interfaces
For each region there are three interfaces:  
**INavigationService**
```csharp
public interface INavigationService
{
    IRegionService Region { get; set; }

    NavigationResponse NavigateAsync(NavigationRequest request);
}
```

*INavigationService* <- **NavigationService**


**IRegionService**
```csharp
public interface IRegionService
{
    Task NavigateAsync(NavigationContext context);

    Task AddRegion(string regionName, IRegionService childRegion);

    void RemoveRegion(IRegionService childRegion);
}
```

*IRegionService* <- **RegionService**


**IRegionManager**
```csharp
public interface IRegionManager
{
    NavigationContext CurrentContext { get; }

    Task NavigateAsync(NavigationContext context);
}
```

*IRegionManager*  
<- BaseRegionManager  
<- <- SimpleRegionManager  
<- <- <- **ContentControlRegionManager**  
<- <- <- **TabRegionManager**  
<- <- StackRegionManager  
<- <- <- **FrameRegionManager**  
            



## Examples
```plaintext
App                     Root-NS                 Root-RS
-> Frame                -> NS1                  -> RS1 (Nested=RS2) -> FrameRegionManager
    -> TabView          -> NS2 (Parent=NS1)     -> RS2 (Nested=RS2) -> TabRegionManager
       -> Tab1          
          -> Frame      -> NS3 (Parent=NS2)     -> RS3              -> FrameRegionManager  
          
Uri = "MainPage/Tab1/TweetsPage"
NS1.Navigate("MainPage/Tab1/TweetsPage")
       |---> RS1.Navigate("MainPage/Tab1/TweetsPage") Navigates the Frame to "MainPage"
                   |---> RS2.Navigate("Tab1/TweetsPage") Navigates the TabView to "Tab1"
                                |---> RS3.Navigate("TweetsPage") Navigates the Frame to "TweetsPage"

Uri = "./Tab1/TweetsPage"
NS1.Navigate("./Tab1/TweetsPage")
       |---> RS1.Navigate("./Tab1/TweetsPage") Does nothing (assumes current page is correct)
                   |---> RS2.Navigate("Tab1/TweetsPage") Navigates the TabView to "Tab1"
                                |---> RS3.Navigate("TweetsPage") Navigates the Frame to "TweetsPage"

Uri = "//ExplorePage/Tab1/TweetsPage"
NS2.Navigate("//ExplorePage/Tab1/TweetsPage")
       |---> NS1.Navigate("ExplorePage/Tab1/TweetsPage")
                   |---> RS1.Navigate("Explore/Tab1/TweetsPage") Navigates Frame to "ExplorePage"
                               |---> RS2.Navigate("Tab1/TweetsPage") Navigates the TabView to "Tab1" 
                                            |---> RS3.Navigate("TweetsPage") Navigates the Frame to "TweetsPage"

Uri = "Tab2/FeedsPage"
NS2.Navigate("Tab2/FeedsPage")
       |---> RS2.Navigate("Tab2/FeedsPage") Navigates the TabView to Tab2 
                    |---> RS3.Navigate("FeedsPage") Navigates the Frame to "FeedsPage"

Uri = "FeedDetailsPage"
NS3.Navigate("FeedDetailsPage")
        |---> RS3.Navigate("FeedDetailsPage") Navigates the Frame to "FeedDetailsPage"

Uri = "../TweetDetailsPage"
NS3.Navigate("../TweetDetailsPage")
        |---> RS3.Navigate("../TweetDetailsPage") Navigates the Frame to "TweetDetailsPage" and removes 1 item from backstack
        
Uri = "/SettingsPage"
NS3.Navigate("/SettingsPage")
        |---> RS3.Navigate("/SettingsPage") Navigates the Frame to "SettingsPage" and clears backstack
        


```


