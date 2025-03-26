using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
public static class RLEParser
{
    // Reads an RLE file from a given path, parses it, and outputs LevelData object containing a list of Vector2 positions for living cells
    /*
     * About RLE
     * RLE:  run length encoded file, this is the format for most shared GOL community files, especially on Golly's open database
     * 
     * RLE files use a compact notation for Conway's Game of Life patterns. The format generally looks like this:
     *      x = 5, y = 5, rule = B3/S23
     *      bo$2bo$3o!
     * 
     *   x = width, y = height - The grid size.
     *   b - Dead cell.
     *   o - Live cell.
     *   $ - Move to the next row.
     *   ! - End of the file.
     *   Numbers before b or o indicate how many times the cell repeats (e.g., 3o means three live cells).
     * 
     */
    public static LevelData ParseLevelDataFromRLEFile(string rleContent, GameManager gameManager) //rle = run length encoded file, this is the format for most shared GOL community files, especially on Golly's open database
    {
        List<Vector3Int> livingCellPositions = new List<Vector3Int>();//set up the vector3int that you'll store living cells in

        //initialize values
        string[] linesInRLE = rleContent.Split('\n'); // split the rle document into an array of strings, one string per line in document.
        string patternData = "";

        //Extract the pattern data
        foreach (string line in linesInRLE)
        {
            if (line.StartsWith("x") || line.StartsWith("#") || line.StartsWith("[")) continue; // skip metadata
            if (line.StartsWith("!"))
            {
                patternData += line.Replace("!", "").Trim();//if the line contains !(indicator of end of file), replace ! with nothing, then remove all white space after that point from the line.
                break;
            }

            patternData += line.Trim();//add each line to the pattern data, with all whitespace before or after the line removed
        }

        //Process pattern line by line
        //MatchCollection matches = Regex.Matches(patternData, @"(\d*[bo$])");//a MatchCollection object represents a set of succe3ssful matches found via the Regex.Matches function; Regex.Matches searches a string (first parameter) for all occurrences of a regular expression and returns all matches as sets
        MatchCollection matches = Regex.Matches(patternData, @"(\d+[bo$.]|[bo$.])");//a MatchCollection object represents a set of succe3ssful matches found via the Regex.Matches function; Regex.Matches searches a string (first parameter) for all occurrences of a regular expression and returns all matches as sets
        /*
         * Regex pattern explanation
         * EG: (\d*[bo$])
         * 
         * 1.   \d* (Optional Number)
         *      \d - Matches any digit (0-9).
         *       * -  Matches zero or more repetitions of the preceding character.
         *          This means it will match numbers if they exist (e.g., 3b, 5o) but also allow single characters (b, o, $).
         *  
         *  2. [bo$] (Match a Single Character)
         *      [bo$] - A character class that matches one of the following three characters:
         *      'b' - Dead cells (background cells, no action needed).
         *      '.' - Dead cells alternative (background cells, no action needed).
         *      'o' - Live cells (we will store these in our JSON).
         *      '$' - New row (moves the cursor to the next line).
         */
        int currentX = 0;
        int currentY = 0;
        int highestYValue = 0;//this will store the highest y value for calculating the height of the tilemap
        int highestXValue = 0;
        foreach (Match match in matches)
        {
            string token = match.Value;
            /*
              About Tokens
                
            A token in this RLE parsing approach is any substring that represents one unit of information about the pattern. Each token follows one of these structures:

                b (Dead Cell)

                Example: "b" (1 dead cell)
                Example: "3b" (3 dead cells)
                Meaning: Skip repeat number of cells.
                o (Live Cell)

                Example: "o" (1 live cell)
                Example: "5o" (5 live cells)
                Meaning: Store repeat number of live cell positions.
                $ (New Line)

                Example: "$" (End current row and move to the next)
                Example: "2$" (Skip 2 rows)
                Meaning: Move the cursor to the start of the next row.
                Combination (e.g., "10b", "7o")

                If a number appears before b or o, it represents how many times that cell type is repeated.

             */
            /*
            int repeat = 1;
            if(token.Length > 1)
            {
                repeat = int.Parse(token.Substring(0, token.Length - 1));
            }
            */
            int repeat = (token.Length > 1) ? int.Parse(token.Substring(0, token.Length - 1)) : 1;
            /*
              About 'repeat'
            The repeat value is the number before b, o, or $ in an RLE token.

            If no number is present, it defaults to 1.
            If a number is present, it tells us how many times the character repeats.

            EG
                "b"	    1 dead cell
                "o"	    1 live cell
                "3b"	3 dead cells
                "5o"	5 live cells
                "$"	    Move to next row
                "2$"	Move down 2 rows

             */

            char symbol = token[token.Length - 1]; //the symbol in question is the last character in the token, earlier characters may be repeat numbers

            if (symbol == 'b' || symbol == '.') // Dead cells (skip)
            {
                currentX += repeat;
            }
            /*
            else if (symbol == 'o') // Live cells
            {
                for (int i = 0; i < repeat; i++)
                {
                    livingCellPositions.Add(new Vector3Int(currentX, currentY, 0));
                    
                    if(currentY > highestYValue) highestYValue = currentY;
                    if(currentX > highestXValue) highestXValue = currentX;

                    currentX++;
                }
            }*/
            else if (symbol == 'o') // Live cells
            {
                for (int i = 0; i < repeat; i++)
                {
                    livingCellPositions.Add(new Vector3Int(currentX++, currentY, 0)); // Increment X inside Add()

                    if (currentY > highestYValue) highestYValue = currentY;
                    if (currentX > highestXValue) highestXValue = currentX;
                }
            }
            else if (symbol == '$') // New row
            {
                //currentY++;
                currentY += repeat;
                currentX = 0;
            }
        }

        //set the other values for your LevelData object
        float widthToHeightRatio = gameManager.tileMapManager.tileMapWidthToHeightRatio;



        int tileMapHeight = Convert.ToInt32(Mathf.Max((float)highestYValue, (float)(highestXValue / widthToHeightRatio)));//returns the largest of the two, either the highestYValue, or the highestXValue/width to height ratio (if structure is far wider than it is long)
        int tileMapHeightWithPadding = Convert.ToInt32(tileMapHeight* gameManager.importedRLEPaddingMultiplier);//returns the largest of the two, either the highestYValue, or the highestXValue/width to height ratio (if structure is far wider than it is long)

        //create padding values
        int paddingAmountY = tileMapHeightWithPadding - tileMapHeight;
        int paddingAmountX = Convert.ToInt32(paddingAmountY * widthToHeightRatio);

        //apply padding
        List <Vector3Int> paddedLivingCellPositions = new List<Vector3Int>();
        foreach(Vector3Int position in livingCellPositions)
        {
            paddedLivingCellPositions.Add(new Vector3Int(position.x + (paddingAmountX/2), position.y + (paddingAmountY/2), 0));
        }
        
        LevelData levelData = new LevelData(tileMapHeightWithPadding, paddedLivingCellPositions);

        return levelData;
    }
}
