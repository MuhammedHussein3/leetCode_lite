﻿using AJudge.Application.DtO.ProblemsDTO;
using AJudge.Application.DTO.ProblemsDTO;
using AJudge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AJudge.Application.services
{
    public interface IProblemService
    {
        Task<OrignalProblems> FetchProblem(FetchProblemDto problemDto);
        Task<bool> ChangeProblemStatement(ProblemDTO problemDTO);
        Task<bool> DeleteProblem(int problemId);
        Task<ProblemDTO> GetProblem(int problemId);
        Task<List<Problem>> GetAllProblems(int problemId);
        Task<ProblemPagination> GetAllProblemsInPage(string sortBy, bool isAsinding = true, int pageNumber = 1, int pageSize = 100);
        Task<List<ProblemDTO>> GetProblemsByTag(string tag);
        Task<List<ProblemDTO>> GetProblemsByDifficulty(string difficulty);
        Task<List<ProblemDTO>> GetProblemsBySource(string source);

        Task<ProblemDetailsDTO?> GetProblemByName(string name, int? userId);
        Task<ProblemDetailsDTO?> GetProblemDetailsAsync(int problemId, int? userId); // New method for problem details by ID

        Task<string> SubmitProblem(int userid,string link,string source,string code,string lang);

    }

}
