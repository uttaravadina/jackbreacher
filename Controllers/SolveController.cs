﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JackBreacher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolveController : ControllerBase
    {
        // POST api/<OcrController>
        [HttpPost]
        public ActionResult<List<string>> Post([FromForm] string matrixString, [FromForm] string sequenceString, [FromForm] int bufferSize)
        {
            if (string.IsNullOrWhiteSpace(matrixString) || string.IsNullOrWhiteSpace(sequenceString) || bufferSize <= 0)
                return Ok(new List<string>() { "Parameters not correct" });

            List<string> matrix = matrixString.Split(",").ToList();
            List<string> sequences = sequenceString.Split(",").ToList();

            // Make sure that each row has the right number of cells and matches the count
            List<string> errors = new List<string>();
            matrix.ForEach(x =>
            {
                if (x.Length % 2 != 0)
                {
                    errors.Add($"Matrix row '{x}' is not a multiple of 2 characters");
                    return;
                }

                if (Math.Floor((double)x.Length / 2) != matrix.Count())
                {
                    errors.Add($"Matrix row '{x}' does permit a square matrix");
                    return;
                }
            });

            // Validate each of the sequences
            sequences.ForEach(x =>
            {
                var parts = x.Split("=").ToList();
                var seq = parts.First();
                var value = parts.ElementAtOrDefault(1);
                int iValue = 0;

                if (seq.Length % 2 != 0)
                {
                    errors.Add($"Sequence '{x}' is not a multiple of two characters");
                    return;
                }

                if (!String.IsNullOrWhiteSpace(value) && !int.TryParse(value, out iValue))
                {
                    errors.Add($"Sequence value for '{x}' is not a valid integer");
                    return;
                }
            });

            // Error out on validation if needed
            if (errors.Count > 0)
                return Ok(errors);


            // Since we got this far, we know we have a square matrix based on comma splices
            int height = matrix.Count;
            int width = height;

            // Createa the solver and send in all the rows
            Solver s = new Solver(height, width) { BufferSize = bufferSize };
            for (int i = 0; i < matrix.Count; i++)
                s.SetRow(i, matrix[i]);

            // Figure out the sequences to solve and send those in
            for (int i = 0; i < sequences.Count; i++)
            {
                var parts = sequences[i].Split("=").ToList();
                var seq = parts.First();
                var value = parts.ElementAtOrDefault(1);
                int iValue = i + 1;
                if (!String.IsNullOrWhiteSpace(value))
                    iValue = int.Parse(value);

                s.AddSequence(seq, iValue);
            }

            // Go ahead and solve it 
            Sequence result = s.Solve();

            // No results means no solution
            if (result == null || result.SolvedValues.Count == 0)
            {
                return Ok(new List<string>() { "No solution found" });
            }

            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            List<string> resultStr = new List<string>();
            // Pretty print the solution
            resultStr.Add($"Best solution:");
            resultStr.Add($"  Solved: {String.Join(", ", result.SolvedValues)} for {result.Value} total");
            resultStr.Add($"  Start Chain: {result.Used}");
            resultStr.Add($"  Solution:");
            result.Cells.ForEach(c => resultStr.Add($"    {c.Value} ({c.Row + 1},{letters[c.Column]})"));

            return Ok(resultStr);
        }
    }
}
