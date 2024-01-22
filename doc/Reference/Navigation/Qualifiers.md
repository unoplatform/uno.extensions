---
uid: Reference.Navigation.Qualifiers
---

# Navigation Qualifiers

| Qualifier |                                                              | Example          |                                                              |
|-----------|--------------------------------------------------------------|------------------|--------------------------------------------------------------|
| ""        | Navigate to page in frame, or open popup                     | "Home"           | Navigate to the HomePage                                     |
| /         | Forward request to the root region                           | "/"<br>"/Login"  | Navigate to the default route at the root of navigation<br>Navigate to LoginPage at the root of navigation |
| ./        | Forward request to child region                              | "./Info/Profile" | Navigates to the Profile view in the child region named Info |
| !         | Open a dialog or flyout                                      | "!Cart"          | Shows the Cart flyout                                        |
| -         | Back (Frame), Close (Dialog/Flyout) or respond to navigation | "-"<br>"--Profile"<br>"-/Login" | Navigate back one page (in a frame)<br>Navigate to Profile page and remove two pages from backstack<br>Navigate to Login page and clear backstack |
