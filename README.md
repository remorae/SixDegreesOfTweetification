[![Build Status](https://travis-ci.com/APlagman/SixDegreesOfTweetification.svg?token=iPhZBdmsACsxxyMy4thY&branch=master)](https://travis-ci.com/APlagman/SixDegreesOfTweetification)
# SixDegreesOfTweetification
A senior project created for the 2018 TEALS conference.

If you are viewing this file locally, please ensure you view the `_docs` folder for the full documentation!

## Core Features
The “Six Degrees” app is a web application which allows users to input:

1. One or more hashtags, and show the most commonly associated hashtags organized by frequency.

2. Two or more hashtags, and show how many tweets it takes to reach one from the other via related hashtags, visualized via a graph of connected nodes.

3. A hashtag, and show the geographic use of that particular hashtag visualized via a country leaderboard.

4. Two Twitter user handles, one as a start point and the other as an endpoint, and show how many people it takes for one to reach the other via social connections (followers, who they are following). This would also be visualized via a graph.

## Organization
The project consists of two key parts: A .NET Core back-end web server and an Angular front-end single-page application.
The back-end code can be found within `src/`, and the front-end code can be found within `src/ClientApp/`.

Users authenticate with the server via credentials and/or their Twitter account in order to use features that are rate-limited by Twitter. No user data is stored besides that needed to authenticate to Twitter. Twitter account linking is authorized to be read-only.
The front-end manages routing and all user-facing content.

The back-end server is in charge of all Twitter API interaction. To improve functionality and useability, a lot of information is cached using a Neo4j graph database.

Generated documentation will be contained within the `_docs/` folder in the root directory (`index.html` is a good starting point).
Markdown articles will be turned into HTML when `docfx` is run in the root directory.

Front-end documentation will be generated when `npm run compodoc-serve` is run in the `src/ClientApp/` directory.

## Links (For generated docs)
**For project setup, please see [the setup guide](./articles/Project-Setup.html).**

For a simplified description of the server API, see [here](./articles/Six-Degrees-API.html).
Likewise, the security-related server information can be found [here](./articles/Security.html).

For an end-user guide, see [here](./articles/User-Guide.html).

For more complete HTML documentation on the back-end, see [here](./api/SixDegrees.html).
Please note that the generated docs for the back-end only contain publicly-visible classes and methods. Most non-public code should be sufficiently documented within the source code itself; the rest is trivial or consists of self-explanatory helper functions.