<style>
  body {
        font-family: Arial, sans-serif;
        font-size: 1rem;
        line-height: 1.3rem;
        }
    h1 {
        color: blue;
        font-size: 1.6rem;
    }
    h2 {
        font-size: 1.4rem;
    }
    h3 {
        font-size: 1.2rem;
    }
</style>

# watsonx Assistant
## Initial setup
### Introduction

Watson Assistant is an AI chatbot that can be used to answer questions, provide information, and help users with tasks. 
It is a powerful tool that can be used in a variety of applications, from customer service to technical support.

### Create a watsonx Assistant instance

- Log into your IBM account and create a new Watson Assistant instance.
- Follow the steps suggested by the IBM website to gain an understanding of what is possible using the Assistant.

Once you have your basic Assistant created, with a few simple actions, it is time to theme your Assistant.  
This is done through the Preview tab.   
Considerations here for the icon are:
 - this **must** be square.
 - the size of the icon must be 64-100px in size.
 - I have found that it is happier when the file is a .jpg rather than a .png.
 - I chose our company logo for the icon, and a colour scheme that matched our company colours.
 - Each time you go into the Preview tab, It appears to complain about the icon - click close on the pop-up, and your avatar will remain unchanged.
 - Choose suggested questions carefully - make sure the bot can actually answer what you are suggesting!
 - If you have a supported service desk, the bot can send them to an agent if the answers are insufficient.
 - If you are using an unsupported service desk, there are 


## ElasticSearch and watsonx Assistant Integration

### Introduction

For ease of use, we set up an ElasticSearch instance to store the data we wanted to query in Watson Assistant.

*This is the recommended way to store and query datasets in Watson Assistant*

Once set up, you need to *immediately* reset the password for the ElasticSearch instance.

Go to your deployment->Security
Note the password down, you will use this to authenticate your API calls in both watson Assistant and code that uploads the index.

Your data can be added to ES in a number of ways, the way we chose to go was with a simple dotNet program that reads Word documents and splits them down into segments.

These segments are the text of the Header, followed by the text of the body of that section.

We chose DocX as the document format in preference to pdf as there is metadata in the DocX format indicating Headings etc.

This made the document split easier to implement.

As I am a dotNet developer, I decided after working with Python, that I would write the code to process and ingest a document into Elasticsearch using a c# command-line program

The code is in the repository: <github>
 
**Don't rush Elastic!**  
It takes time for passwords to percolate (or it did for me!) - it took a day for the elastic password reset to work it's way through to the API, so be patient!

I have found it easiest to use the elastic user/password for authentication, as that works clearest.

Ensure you use the same version SDK as your Elastic resource, as there are likely to be breaking changes.

### ***BEWARE*** of ElasticSearch costs.
We tried the Azure Elasticsearch service, which created an Enterprise elastic search instance

- This was overkill for our needs, and we were charged £560 ( ~$620) for a month's usage - when, in fact,
  a Standard single zone instance with only a hot zone is more than sufficient for a proof of concept.
-
    - We have now moved to a single zone instance, which is more than sufficient for our needs, and costs £~17 ($~20) a
      month.
    - We have now reduced it down to the minimal machine with 37kB of storage and 1GB of RAM, which is slower, but for a proof of concept, enough. It also reduced the cost .
 
### Further reading
After the fact, I have discovered documentation and samples on how to do all this in github, which would have cut the learning down to a few hours, rather than the days it took me to figure it out.

The github link is: https://github.com/watson-developer-cloud/assistant-toolkit/tree/master  
In the /integrations/extensions/docs/elasticsearch-install-and-setup folder, there is documentation on:
- setting up elastic search
- how to ingest documents using python, and,
- how to make searches using
  - ELSER, 
  - Dense vectors
- and
- adding filtering to the search
- how to run queries over multiple indices within Elasticsearch
- 

# Watsonx Assistant:
Once you have the data in Elasticsearch, you can use Watson Assistant to query the data.
In the Integration section:

    1. choose the Search extension
    2. choose the environment
    3. choose the Elasticsearch integration
    4. enter the URL, port, authentication type and authentication details for your Elasticsearch instance
    5. enter the index name, title, body and optionally "url" fields from the index
    6. Save the configuration
 
You need to modify the Action->Set by Assistant->No Matches to query the ElasticSearch instance and return the results to the user, so
    
- Set the And Then condition to be Search for the answer
                                                           

# watsonx Assistant in Microsoft Teams
To integrate Watson Assistant with Microsoft Teams, you need to create a bot in Microsoft Teams, and then use the Bot ID
and Bot Secret to authenticate the Watson Assistant instance with Microsoft Teams.

This is a relatively easy process, and the documentation is clear and easy to follow.

You may need to give it 24 hours to propagate the changes, as at first no AI searching was available, but the following day, it was working fine.

You need permissions within your Entra AD to upload an App to Teams, so you may need to get your IT department to authorise the app for you.

# watsonx in Jira?
Jira is a powerful tool for managing projects, and has a strong security model, which makes integrating watsonx Assistant into the Jira portal impossible.  
If you want to integrate Jira into your own website, then they provide widgets that you can use to do this - but there is no way to integrate a third-party widget into Jira's portal.

A future move will be to create our own portal with both Jira widgets and our watsonx Assistant on board, thus getting the best of both worlds.
The web page will need to be behind a login screen, so that only authorised users can access the page.
 
# watsonx in a Website
This is possibly the easiest integration to do, as you can add a short sequence of javascript to the page to integrate the Assistant into your website.

# Further things to consider
1. ElasticSearch can be installed into a Linux LPAR on a Power9/Power10 machine.  
   - This would be a more cost-effective solution than using a cloud-based solution.
   - This would also allow you to ramp up the ElasticSearch instance from the minimal machine as provided by Elastic - this would
enhance the speed and quality of the searches.  
   - Introduction of dense vectors, nested indices and filtering would also enhance the search capabilities of the ElasticSearch instance.  
All this would be a low-cost option. ElasticSearch is free to use, and the only cost would be the hardware and the electricity to run it.
2. Instead of using the built-in LLM of the Assistant, it is feasible to host your own LLM on your Power10, and integrate that with the Assistant

