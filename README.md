# uSync Git Rules [EXPERIMENT]

Wondering what we could do to have uSync perform actions based on the status of the repo. 

> [!WARNING]
> This is an experiement code is not supported fully tested, etc. 

# things this repo does. 

1. It writes the last commit SHA value to disk everytime a uSync import is peerformed.
2. It checks at startup to see if the repo is clean (no uncommited files).
3. it compares the last synced SHA to the latest commit in the repo.
4. If they are diffrent it runs a full uSync import 


# things it could do. 

1. [x] Perform diffrent types of import based on config.
2. [ ] Only perform the import if there have been changes files in the uSync folder since the last sync.
3. [ ] Look for merge commits, and only do this if there is a merge commit beyond the last imported SHA (so only import after merges)
4. [ ] Have some fancy rule base that let you work it all out

