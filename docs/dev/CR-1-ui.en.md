In the scope of this project we are building an application that allows regular users to analyze car sale listings, identify potential risks, spot inconsistencies in the data, and receive purchase recommendations. More detailed information is available in the `docs` directory.

A prototype that performs an end-to-end check has already been implemented. However, the current implementation turned out to be convoluted and hard to understand. We therefore plan to refactor the code to improve its structure and readability, and also to improve the user experience to make the application more convenient and intuitive.

We will roll out changes top-down, starting with UI improvements and then moving on to optimizing the internal application logic. This will allow us to provide a smoother and more pleasant experience for users while also increasing the overall efficiency of the application.

Review the current UI code. At the moment there are 2 pages:
- a home page containing a button to sign in with a Google account
- a dashboard used to enter information and display the results of car listing analysis.

We need to get rid of this structure and move to a single page that combines the functionality of both pages. This page will support Google account sign-in. That functionality should be positioned in the center of the page. An unauthenticated user should not be able to analyze listings — they should only see the sign-in button. After successful authentication the user is redirected to the same page, but now with access to the listing analysis functionality. That is, the sign-in button area is replaced with a more extensive interface for entering information, viewing analysis results, and browsing the user's analysis history.

The data-entry area should consist of several components:
1. A multi-line text input for pasting the text of a car sale listing. It should support basic text formatting such as bold, italic, and underline so that users can highlight important parts of the listing, as well as lists and other formatting elements for better information organization. Most likely some kind of markdown editor that supports these features would be appropriate. This will allow users to structure listing information more effectively and highlight key points for analysis.
2. A button for attaching images. This is necessary so that users can attach photos of the car or screenshots of the listing that may be useful for analysis. Attached images should be displayed as thumbnails below the text input, and users should be able to remove them as needed as well as view them at full size by clicking on a thumbnail.
3. A button for attaching a link to the otomoto.pl website. At the moment we only support this site because we can parse it and extract information for analysis. A user can attach only one link, and it must be valid. A new link replaces the previous one if one has already been attached. The attached link should be displayed below the text input, and users should be able to remove it as needed.

The actual data contract sent from the UI to the backend for analysis will include the following fields:
- `description`: the text the user pastes into the multi-line text input. It must preserve the original text formatting in markdown format so that the backend can correctly interpret and analyze it.
- `images`: the collection of images the user attaches. Maximum number of images is 5 (should be configurable via service settings). Maximum image size is 2560 KB (also configurable via service settings).
- `link`: a text field containing the link. In general this field is generic in the data contract. However, on the backend we will verify that the link actually points to otomoto.pl and extract the necessary information for analysis. If for some reason a link to another site is received, we will ignore it and process the listing based only on the text and images, if any are present.

The same page should also display the user's analysis history as a paginated list (no more than 5 analyses per page). Clicking on a list item loads the analysis result via a separate request and displays it in a modal window with a dimmed background around it.

At this stage, changes should be made exclusively to the user interface. The application logic and backend interaction will be refined and implemented in the next stage. It is important that the current implementation is clean, clear, and easy to maintain so that it will be straightforward to add new features and improve existing ones in the future.
