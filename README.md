# Lab5
The program consists of a TextBox where you enter a web address (URL) and one button "Extract". When you press the button (or enter), the program loads download the HTML code from the URL you entered, search this for links to images and shows all the links it finds in a multiline TextBox. There is one Label showing how many image links were found on the page. There is a "Save Images" button that opens one FolderBrowserDialog where you can select a folder where you want to save all the images. When you have selected a folder (and clicked "OK"), the program retrieves everyone asynchronously images via the links and save down to files in the order the downloads are completed.