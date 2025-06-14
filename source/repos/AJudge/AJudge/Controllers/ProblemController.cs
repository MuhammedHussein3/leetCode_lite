﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AJudge.Application.DTO.ProblemsDTO;
using AJudge.Application.services;
using AJudge.Domain.RepoContracts;
using AJudge.Domain.Entities;
using AJudge.Application.DtO.ProblemsDTO;
using AJudge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AJudge.Application.DTO.GroupDTO;
using System;
using System.Security.Claims;
using static AJudge.Domain.Entities.Problem;

using Azure;
using System.Net;
using Microsoft.AspNetCore.Identity;
using AJudge.Application.DtO.CommentDTO;
using AJudge.Infrastructure.Repositories;

namespace AJudge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProblemController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProblemService _problemService;
        private readonly IGroupServices _groupServices;
        private readonly ApplicationDbContext _context;

        public ProblemController(IUnitOfWork unitOfwork, ApplicationDbContext context, IProblemService problemService, IGroupServices groupServices)
        {
            _unitOfWork = unitOfwork;
            _context = context;
            _problemService = problemService;
            _groupServices = groupServices;
        }

        /// <summary>
        /// Retrieves detailed information about a specific problem.
        /// </summary>
        /// <param name="problemId">The unique identifier of the problem.</param>
        /// <returns>
        /// Returns the detailed problem information if found; otherwise, returns a 404 Not Found response.
        /// If the user is authenticated, their user ID is used to tailor the response (e.g., submission state).
        /// </returns>
        [HttpGet("{problemId}")]
        [ProducesResponseType(typeof(ProblemDetailsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProblemDetails(int problemId)
        {
            int? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim?.Value, out int parsedId))
                {
                    userId = parsedId;
                }
            }

            ProblemDetailsDTO? problemDetailsDTO = await _problemService.GetProblemDetailsAsync(problemId, userId);
            if (problemDetailsDTO == null)
                return NotFound(new { message = "No such problem found." });

            return Ok(problemDetailsDTO);
        }
        /// <summary>
        /// Fetches a problem from an external source (like CSES), adds it to the contest, and returns detailed problem info.
        /// </summary>
        /// <param name="problemDto">Problem details including source, link, problem ID, and contest ID.</param>
        /// <returns>Returns detailed problem info on success or an error message.</returns>
        [HttpPost("CSESProblem")]
        [Authorize]
        [ProducesResponseType(typeof(ProblemDetailsDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> FetchP([FromBody] FetchProblemDto problemDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "invalid data " });
            }
            try
            {
                var Contest = _context.Contests.FirstOrDefault(x => x.ContestId == problemDto.ContestID);
                int userId = GetUserIdFromToken();
                if (Contest == null)
                {
                    return BadRequest(new { message = "invalid ContestId" });
                }
                int groupID = Contest.GroupId;
                if (!_groupServices.UserManagerInGroup(groupID, userId).Result)
                {
                    return BadRequest(new { message = "You are not a manager in this group" });
                }
                var problem = await _problemService.FetchProblem(problemDto);
                if (problem == null)
                {
                    return BadRequest(new { message = "problem not added" });
                }
                Problem NewProblem = new Problem
                {
                    ProblemName = problem.ProblemName,
                    ProblemSource = problemDto.ProblemSource,
                    Description = problem.Description,
                    InputFormat = problem.InputFormat,
                    OutputFormat = problem.OutputFormat,
                    ProblemLink = problem.ProblemLink,
                    Rating = problem.Rating,
                    ContestId = problemDto.ContestID,
                    numberOfTestCases = problem.numberOfTestCases,
                    ProblemSourceID = problem.ProblemSourceID,
                    TestCases = problem.TestCases.Select(tc => new TestCase
                    {
                        Input = tc.Input,
                        Output = tc.Output
                    }).ToList(),
                };
                ProblemDetailsDTO problemDTO = ProblemDetailsDTO.ConvertToProblemDetalsDTO(
                    NewProblem,
                    "Not Submitted",
                    new List<string>(),
                   problem.TestCases.Select(tc => new InputOutputTestCasesDTO
                   {
                       Input = tc.Input,
                       Output = tc.Output
                   }).ToList()
                );
                try
                {
                    Console.Beep();
                    Console.Beep();
                    await _context.Problems.AddAsync(NewProblem);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "error occure", error = ex.Message });
                }
                return Ok(problemDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "error occure", error = ex.Message });
            }
        }
        /// <summary>
        /// Submit a solution code for a problem.
        /// </summary>
        /// <param name="submit">Contains problem link and submitted code.</param>
        /// <returns>Returns result of the submission or error.</returns>
        [HttpPost("Sumbit")]
        [Authorize]
        public async Task<IActionResult> SubmitProblem([FromBody] SubmitDTO submit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "invalid data " });
            }
            try
            {
                int userId = GetUserIdFromToken(); // Temporary hardcoded userId, replace with GetUserIdFromToken() if needed
                var result = await _problemService.SubmitProblem(userId, submit.ProblemLink, submit.Source, submit.Code, submit.lang);

                if (result == null)
                {
                    return BadRequest(new { message = "problem not added" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "error occure", error = ex.Message });
            }
        }

        //    [HttpGet("GetProblems")]
        //    public async Task<ActionResult<IEnumerable<Problem>>> GetProblems(
        //        [FromQuery] string tags,
        //        [FromQuery] int? rating,
        //        [FromQuery] int page = 1,
        //        [FromQuery] int pageSize = 20,
        //        [FromQuery] string status = null)
        //    {
        //        try
        //        {
        //            if (page < 1) page = 1;
        //            if (pageSize < 1 || pageSize > 100) pageSize = 20;

        //            var query = _context.Problems
        //                .Include(p => p.ProblemTags)
        //                .ThenInclude(pt => pt.Tag)
        //                .AsQueryable();

        //            if (!string.IsNullOrWhiteSpace(tags))
        //            {
        //                var tagsList = tags.Split(" and ", StringSplitOptions.RemoveEmptyEntries)
        //                    .Select(t => t.Trim())
        //                    .Where(t => !string.IsNullOrWhiteSpace(t))
        //                    .ToList();

        //                if (tagsList.Any())
        //                {
        //                    foreach (var tag in tagsList)
        //                    {
        //                        var tagLower = tag.ToLower();
        //                        query = query.Where(p => p.ProblemTags.Any(pt =>
        //                            EF.Functions.Like(pt.Tag.Name.ToLower(), tagLower)));
        //                    }
        //                }
        //            }

        //            if (rating.HasValue)
        //            {
        //                query = query.Where(p => p.Rating == rating.Value);
        //            }

        //            if (!string.IsNullOrWhiteSpace(status))
        //            {
        //                if (Enum.TryParse<ProblemStatus>(status, true, out var problemStatus))
        //                {
        //                    query = query.Where(p => p.Status == problemStatus);
        //                }
        //                else
        //                {
        //                    return BadRequest(new { error = $"Invalid problem status: {status}" });
        //                }
        //            }

        //            var totalCount = await query.CountAsync();

        //            var problems = await query
        //                .Skip((page - 1) * pageSize)
        //                .Take(pageSize)
        //                .ToListAsync();

        //            var problemDtos = problems.Select(p => new Problem
        //            {
        //                ProblemId = p.ProblemId,
        //                Rating = p.Rating,
        //                Status = p.Status,
        //              //  stringTags = p.ProblemTags.Select(pt => pt.Tag.Name).ToList()
        //            });

        //            return Ok(new
        //            {
        //                data = problemDtos,
        //                pagination = new
        //                {
        //                    currentPage = page,
        //                    pageSize = pageSize,
        //                    totalItems = totalCount,
        //                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        //                }
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            return StatusCode(500, new { message = "error occure", error = ex.Message });
        //        }
        //    }



        //    [HttpGet("{problemName}")]

        //    public async Task<IActionResult> GetProblem(string problemName)
        //    {
        //        int? userId = null;
        //        if (User.Identity != null && User.Identity.IsAuthenticated)
        //        {
        //            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        //            if(userIdClaim != null && int.TryParse(userIdClaim?.Value, out int parsedId))
        //            {
        //                userId = parsedId;  
        //            }
        //        }
        //        ProblemDetailsDTO? problemDEtailsDTO = await _problemService.GetProblemByName(problemName, userId);



        //        if (problemDEtailsDTO == null)
        //            return NotFound("No such Problem");
        //        return Ok(problemDEtailsDTO);
        //    }





        //    [HttpGet("page")]
        //    public async Task<IActionResult> GetAllProblemsPerSpecificPage(string? sortBy, bool isAssending = true, int pageNumber = 1, int pageSize = 100)
        //    {

        //        var sortByy = typeof(Problem).GetProperty(sortBy);
        //        if(sortByy == null)
        //        {
        //            return BadRequest("No such property");
        //        }
        //        var page = await _problemService.GetAllProblemsInPage(sortBy, isAssending, pageNumber, pageSize);


        //        if (page == null)
        //            return NotFound("No such Page");

        //        var DisPage=ProblemPageDTO.ConvertToProblemPageDTO(page);


        //        return Ok(DisPage);
        //    }



        //        //[HttpPost("CreateProblem")]
        //        //public async Task<IActionResult> CreateProblem([FromBody] ProblemDTO problem)
        //        //{
        //        //    //var result = await _problemService.CreateProblem(problemDTO);
        //        //    //return Ok(result);
        //        //    return Ok();
        //        //}



        //        [HttpPost("CreateProblem")]
        //    public async Task<IActionResult> CreateProblem([FromBody] ProblemDTO problemDto)
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(new { message = "invalid data " });
        //        }

        //        try
        //        {
        //            var problem = new Problem
        //            {
        //                ProblemSource = problemDto.ProblemSource,
        //                ProblemLink = problemDto.problemLink,
        //                ProblemName = problemDto.ProblemName,
        //                Rating = problemDto.Rating,

        //                ProblemSourceID = "ProblemSourceID",
        //                ContestId = problemDto.ContestId,
        //                Description = "Description Placeholder",
        //                InputFormat = "Input Format Placeholder",
        //                OutputFormat = "Output Format Placeholder",
        //                numberOfTestCases = problemDto.NumberOfTestCases
        //            };

        //            await _context.Problems.AddAsync(problem);
        //            await _context.SaveChangesAsync();

        //            var problemDetails = ProblemDetailsDTO.ConvertToProblemDetalsDTO(
        //                problem,
        //                "Not Submitted",
        //                new List<string>(),
        //               new List<InputOutputTestCasesDTO>() 
        //            );

        //            return CreatedAtAction(nameof(GetProblem), new { problemName = problem.ProblemName }, new
        //            {
        //                message = "problem added ",
        //                problem = problemDetails
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            return StatusCode(500, new { message = "error occure", error = ex.Message });
        //        }
        //    }




        //    [HttpPut("{id}")]
        //    public async Task<IActionResult> UpdateProblem(int id, [FromBody] ProblemDTO problemDto)
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(new { message = "invalid data " });

        //        }

        //        try
        //        {
        //            var existingProblem = await _context.Problems
        //                .FirstOrDefaultAsync(p => p.ProblemId == id);

        //            if (existingProblem == null)
        //            {
        //                return NotFound(new { message = "invalid data " });

        //            }


        //            existingProblem.ProblemSource = problemDto.ProblemSource ?? existingProblem.ProblemSource;
        //            existingProblem.ProblemLink = problemDto.problemLink ?? existingProblem.ProblemLink;
        //            existingProblem.ProblemName = problemDto.ProblemName ?? existingProblem.ProblemName;
        //            existingProblem.Rating = problemDto.Rating > 0 ? problemDto.Rating : existingProblem.Rating;
        //            existingProblem.ProblemSourceID = problemDto.ProblemSourceID ?? existingProblem.ProblemSourceID;
        //            existingProblem.ContestId = problemDto.ContestId > 0 ? problemDto.ContestId : existingProblem.ContestId;
        //            existingProblem.numberOfTestCases = problemDto.NumberOfTestCases > 0 ? problemDto.NumberOfTestCases : existingProblem.numberOfTestCases;
        //            existingProblem.Description = "string...."; 
        //            existingProblem.InputFormat = "string....";
        //            existingProblem.OutputFormat ="string....";

        //            _context.Problems.Update(existingProblem);
        //            await _context.SaveChangesAsync();

        //            var problemDetails = ProblemDetailsDTO.ConvertToProblemDetalsDTO(
        //                existingProblem,
        //                "Not Submitted",
        //                new List<string>(),
        //                new List<InputOutputTestCasesDTO>()
        //            );

        //            return Ok(new
        //            {
        //                message = "problem updated ",

        //                problem = problemDetails
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            return StatusCode(500, new { message = "error occure", error = ex.Message });
        //        }
        //    }



        //    [HttpDelete("{id}")]
        //    public async Task<IActionResult> DeleteProblem(int id)
        //    {
        //        try
        //        {
        //            var problem = await _context.Problems
        //                .FirstOrDefaultAsync(p => p.ProblemId == id);

        //            if (problem == null)
        //            {
        //                return NotFound(new { message = "invalid data " });
        //            }

        //            _context.Problems.Remove(problem);
        //            await _context.SaveChangesAsync();

        //            return Ok(new { message = "problem deleted " });
        //        }
        //        catch (Exception ex)
        //        {
        //            return StatusCode(500, new { message = "error occure ", error = ex.Message });
        //        }
        //    }

        //}
        [HttpPut("UpdateProblem{id}")]
        public async Task<IActionResult> UpdateProblem(int id, [FromBody]UpdateProblemDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            }

            Problem? problem = await _unitOfWork.Problem.GetById(id );
            if (problem == null)
                return NotFound("No such problem");

            // Check if current user has permission to update this problem
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            _unitOfWork.Attach(problem);

            problem.Description = request.Description;
            problem.numberOfTestCases = request.numberOfTestCases;       

            
            await _unitOfWork.CompleteAsync();

            var response = ProblemResponseDto.ConvertToProblemResponseDTO(problem);
            return Ok(response);
        }

        private int GetUserIdFromToken()
        {
            if (HttpContext.User.Identity is ClaimsIdentity identity)
            {
                var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }
            throw new UnauthorizedAccessException("User ID not found in token.");
        }
    }

    public class SubmitDTO
    {
        /// <summary>
        /// link of problem
        /// </summary>
        public string ProblemLink { get; set; }
        /// <summary>
        /// content of code submission
        /// </summary>
        public string Code { get; set; }

        public string lang { get; set; }
        public string Source { get; set; }
    }
}