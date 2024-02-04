using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrackVisuData
{
    public class Csv
    {
        List<List<string>> csv_grid = new List<List<string>>(); // [row][column]
        uint decimals = 0;
        int longest_entry = 0;

        int current_row = 0;
        int current_column = 0;

        public Csv(uint columns, uint rows, uint decimals = 2)
        {
            // Fill the grid with empty strings
            csv_grid = new List<List<string>>();
            for (int row_index = 0; row_index < rows; row_index++)
            {
                List<string> row = new List<string>();
                for (int column_index = 0; column_index < columns; column_index++)
                    row.Add("");
                csv_grid.Add(row);
            }

            this.decimals = decimals;
        }

        public void Set(int column, int row, string content)
        {
            if (row >= csv_grid.Count)
                Debug.LogError($"Trying to set row {row} in a csv with only {csv_grid.Count} rows.");
            if (column >= csv_grid[0].Count)
                Debug.LogError($"Trying to set column {column} in a csv with only {csv_grid[0].Count} columns.");

            csv_grid[row][column] = content;
            if (content.Length > longest_entry)
                longest_entry = content.Length;
        }
        public void Set(int column, int row, float content)
        {
            string float_str = content.ToString("F" + decimals);
            Set(column, row, float_str.Replace(",", "."));
        }
        public void NextRow()
        {
            current_column = 0;
            current_row++;
        }
        public void NextColumn(string content)
        {
            Set(current_column++, current_row, content);
        }
        public void NextColumn(float content)
        {
            Set(current_column++, current_row, content);
        }

        public override string ToString()
        {
            string csv_str = "";
            int entry_length = longest_entry + 1;

            for (int row_index = 0; row_index < csv_grid.Count; row_index++)
            {
                List<string> row = csv_grid[row_index];
                for (int column_index = 0; column_index < row.Count; column_index++)
                {
                    if (column_index != 0)
                        csv_str += ", ";
                    csv_str += FitStr(row[column_index], entry_length);
                }
                csv_str += "\n";
            }

            return csv_str;
        }

        private string FitStr(string str, int target_len)
        {
            return str + new string(' ', target_len - str.Length);
        }
    }
}