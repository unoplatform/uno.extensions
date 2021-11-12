

# Navigation Regions
Application is made up of a series of containers, such as a Frame, that support navigation. These are referred to as Regions

There are three types of containers that are currently supported, with others able to be supported by extending the BaseRegion class (or the SimpleRegion or StackRegion sub classes):  
**Frame** - Supports forward and backward navigation  
**TabView** - Supports forward navigation between tabs    
**ContentControl** - Supports forward navigation to show new content  


## Attributes
A region needs to be defined in XAML using one of these attached properties

**IsRegion = true/false**
This defines a region that doesn't have a name and the routing to this region is implicit. 

**RegionName = "region"**
This defines a region that has a name and where the name needs to be included in any navigation route.

**CompositeRegion = {x:Bind ControlName}**
This creates a composite region made up of multiple regions. For example this could be a TabBar and a ContentControl, or a NavigationView and a Grid. This attribute can be applied to **either** control but should only be applied to **one** of the controls.


## Navigation Interfaces
For each region there are three interfaces:  
**INavigationService**
```csharp
public interface INavigationService
{
    NavigationResponse NavigateAsync(NavigationRequest request);
}
```

**IRegionNavigationService**
```csharp
public interface IRegionNavigationService : INavigationService
{
    Task Attach(string regionName, IRegionNavigationService childRegion);

    void Detach(IRegionNavigationService childRegion);
}
```

*INavigationService* <- *IRegionNavigationService* <- **NavigationService**


**IRegion**
```csharp
public interface IRegion
{
    NavigationContext CurrentContext { get; }

    Task NavigateAsync(NavigationContext context);
}
```

*IRegion*  
<- BaseRegion  
<- <- SimpleRegion  
<- <- <- **ContentControlRegion**  
<- <- <- **TabRegion**  
<- <- StackRegion  
<- <- <- **FrameRegion**  
            



## Examples
```plaintext
App                     Root-NS
-> Frame                -> NS1 (Parent=Root-NS) (Nested=RS2)    -> FrameRegion (Frame)
    -> TabView          -> NS2 (Parent=NS1)     (Nested=RS3)    -> TabRegion (TabView)
       -> Tab1          
          -> Frame      -> NS3 (Parent=NS2)                     -> FrameRegion (Frame)  
          
Uri = "MainPage/Tab1/TweetsPage"
Root-NS.Navigate("./MainPage/Tab1/TweetsPage")
       |---> NS1.Navigate("MainPage/Tab1/TweetsPage") Navigates the Frame to "MainPage"
                   |---> NS2.Navigate("Tab1/TweetsPage") Navigates the TabView to "Tab1"
                                |---> NS3.Navigate("TweetsPage") Navigates the Frame to "TweetsPage"

Uri = "./Tab1/TweetsPage"
NS1.Navigate("./Tab1/TweetsPage") Does nothing (assumes current page is correct)
       |---> NS2.Navigate("Tab1/TweetsPage") Navigates the TabView to "Tab1"
                |---> NS3.Navigate("TweetsPage") Navigates the Frame to "TweetsPage"

Uri = "//ExplorePage/Tab1/TweetsPage"
NS2.Navigate("//ExplorePage/Tab1/TweetsPage")
       |---> NS1.Navigate("Explore/Tab1/TweetsPage") Navigates Frame to "ExplorePage"
               |---> NS2.Navigate("Tab1/TweetsPage") Navigates the TabView to "Tab1" 
                            |---> NS3.Navigate("TweetsPage") Navigates the Frame to "TweetsPage"

Uri = "Tab2/FeedsPage"
NS2.Navigate("Tab2/FeedsPage") Navigates the TabView to Tab2 
        |---> NS3.Navigate("FeedsPage") Navigates the Frame to "FeedsPage"

Uri = "FeedDetailsPage"
NS3.Navigate("FeedDetailsPage") Navigates the Frame to "FeedDetailsPage"

Uri = "../TweetDetailsPage"
NS3.Navigate("../TweetDetailsPage") Navigates the Frame to "TweetDetailsPage" and removes 1 item from backstack
        
Uri = "/SettingsPage"
NS3.Navigate("/SettingsPage") Navigates the Frame to "SettingsPage" and clears backstack

```


