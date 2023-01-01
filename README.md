# LegoStudioXMLMerger
A simple merging tool for multiple LEGO Studio kit XML files. Grab the XML files you want from LEGO studio, and put them into this tool to merge it into one set. Useful for when you want to use parts from multiple sets, but don't want to manually configure your palette.

## Usage
```
LegoStudioXMLMerger [space-separated list of XML files] [-s|-nq]
```

### Options:
`-s` Show stack full trace on file save exceptions
`-nq` Omit quantities from output XML

Upon running, the program will display output of the new data to be inserted to the XML. You can then specify a file name and output directory. (if none are specified, the program will generate a file name for you and place the file in the program directory's `output` folder).

You can then use LEGO Studio's config to then upload the new merged `XML` file!
