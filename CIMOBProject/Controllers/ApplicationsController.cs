﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CIMOBProject.Data;
using CIMOBProject.Models;
using Microsoft.AspNetCore.Authorization;
using CIMOBProject.Services;
using System.Security.Claims;
using System.Collections.Generic;

namespace CIMOBProject.Controllers
{
    /// <summary>
    /// This controller is responsible for all the actions related to applications.
    /// </summary>
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private EmailSender emailSender;
        private const int ASSIGNED_ID = 2;

        /// <summary>
        /// Initializes controller with the pretended context.
        /// </summary>
        /// <param name="context"></param>
        public ApplicationsController(ApplicationDbContext context)
        {
            _context = context;
            emailSender = new EmailSender();
        }

        /// <summary>
        /// Indexes all the applications from the latest Edital, only employees can access this action.
        /// </summary>
        /// <param name="employeeId">Employee id, generally is used the logged in employee.</param>
        /// <returns>Index view.</returns>
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Index(String employeeId)
        {
            Edital lastEdital = _context.Editals.Last();
            if(lastEdital == null)
            {
                return View(new List<Application>());
            }
            DateTime lastOpenDate = lastEdital.OpenDate;
            DateTime lastCloseDate = lastEdital.CloseDate;
            var applications = _context.Applications.Include(a => a.ApplicationStat)
                .Include(a => a.Employee).Include(a => a.Student).Include(a => a.BilateralProtocol1)
                .Include(a => a.BilateralProtocol2).Include(a => a.BilateralProtocol3)
                .Where(a => a.CreationDate >= lastOpenDate && a.CreationDate <= lastCloseDate);

            if(_context.Editals.Count() > 1)
            {
                Edital pendingEdital = _context.Editals.OrderByDescending(e => e.CloseDate).Skip(1).Take(1).FirstOrDefault();
                DateTime pendingOpenDate = pendingEdital.OpenDate;
                DateTime pendingCloseDate = pendingEdital.CloseDate;
                var lastApplications = _context.Applications.Include(a => a.ApplicationStat)
                    .Include(a => a.Employee).Include(a => a.Student).Include(a => a.BilateralProtocol1)
                    .Include(a => a.BilateralProtocol2).Include(a => a.BilateralProtocol3)
                    .Where(a => a.CreationDate >= pendingOpenDate && a.CreationDate <= pendingCloseDate);
                ViewData["lastApplications"] = lastApplications;
            }            

            var interviews = _context.Interviews.Include(i => i.Application).ThenInclude(a => a.Student)
                .Include(i => i.Employee).Where(i => i.InterviewDate >= DateTime.Now)
                .OrderByDescending(i => i.InterviewDate);
            ViewData["Interviews"] = interviews;

            return View(await applications.ToListAsync());
        }


        ///<summary>
        ///The objective of this method is to assign an employee to an application who will later evaluate it.
        ///</summary>
        /// <param name="employeeId">A String that represents the id of the current employee.</param>
        /// <param name="applicationId">A int that represents the id of the application which the employee wants to evaluate.</param>
        public async Task<IActionResult> AssignEmployee(String employeeId, int applicationId)
        {
            var application = _context.Applications.SingleOrDefault(a => a.ApplicationId == applicationId);


            if (application.EmployeeId != null)
            {
                return RedirectToAction("Application", "Home", new { message = "Candidatura já está a ser avaliada." });
            }

            application.EmployeeId = employeeId;
            application.ApplicationStatId = ASSIGNED_ID;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Applications", new { employeeId = employeeId });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.ApplicationStat)
                .Include(a => a.Employee)
                .Include(a => a.Student)
                .ThenInclude(s => s.CollegeSubject)
                .Include(a => a.BilateralProtocol1)
                .Include(a => a.BilateralProtocol2)
                .Include(a => a.BilateralProtocol3)
                .SingleOrDefaultAsync(m => m.ApplicationId == id);
            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        /// <summary>
        /// Action that leads to Application creation view.
        /// Receives id of the user who's getting the appplication. While this is supposed to be
        /// a student for our use case, it might change or even be used for employees later on.
        /// This view will warn the user if applications are closed (from Edital).
        /// If the user already has applied he will be redirected to <c>Details(int? id)</c>.
        /// </summary>
        /// <param name="userId">User who's applying.</param>
        /// <returns>Application creation view.</returns>
        public IActionResult Create(string userId)
        {
            var student = _context.Students.Include(s => s.CollegeSubject).Where(s => s.Id == userId).SingleOrDefault();
            var applicationInCurrentEdital = _context.Applications.Where(a => a.StudentId == userId
                && (a.CreationDate < _context.Editals.OrderBy(e => e.Id).Last().CloseDate)
                && (a.CreationDate > _context.Editals.OrderBy(e => e.Id).Last().OpenDate)).SingleOrDefault();
            if (DateTime.Now < _context.Editals.OrderByDescending(e => e.Id).First().OpenDate)
            {
                return RedirectToAction("Application", "Home", new { message = "As candidaturas serão disponibilizadas no dia " + _context.Editals.OrderByDescending(e => e.Id).First().OpenDate.ToString("MM/dd/yyyy") });
            }
            if (DateTime.Now > _context.Editals.OrderByDescending(e => e.Id).First().CloseDate)
            {
                return RedirectToAction("Application", "Home", new { message = "Já terminou a data de entrega das candidaturas (" + _context.Editals.OrderByDescending(e => e.Id).First().CloseDate.ToString("MM/dd/yyyy") + ") para o processo outgoing" });
            }

            if (applicationInCurrentEdital != null)
            {
                return RedirectToAction("Details", "Applications", new { id = applicationInCurrentEdital.ApplicationId });
            }
            else
            {
                ViewData["BilateralProtocol1Id"] = new SelectList(_context.BilateralProtocols.Where(p => p.SubjectId == student.CollegeSubjectId), "Id", "Destination");
                ViewData["BilateralProtocol2Id"] = new SelectList(_context.BilateralProtocols.Where(p => p.SubjectId == student.CollegeSubjectId), "Id", "Destination");
                ViewData["BilateralProtocol3Id"] = new SelectList(_context.BilateralProtocols.Where(p => p.SubjectId == student.CollegeSubjectId), "Id", "Destination");
                ViewData["StudentId"] = userId;
                ViewData["ApplicationStatId"] = 1;
                ViewData["EmployeeId"] = "";
                ViewData["CreationDate"] = DateTime.Now;
                loadHelp();
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApplicationId,StudentId,ApplicationStatId,EmployeeId,BilateralProtocol1Id,BilateralProtocol2Id,BilateralProtocol3Id,CreationDate,ArithmeticMean,ECTS,MotivationLetter,Interview,FinalGrade,Documents,Documents,Motivations")] Application application)
        {
            var student = _context.Students.Include(s => s.CollegeSubject).Where(s => s.Id == application.StudentId).SingleOrDefault();
            if (ModelState.IsValid)
            {
                _context.Add(application);
                await _context.SaveChangesAsync();
                Student newStudent = await _context.Students.Where(s => s.Id.Equals(application.StudentId)).FirstOrDefaultAsync();
                await emailSender.Execute("Candidatura Submetida", "Saudações, a sua candidatura foi submetida no sistema com sucesso, boa sorte!", newStudent.Email);
                _context.ApplicationStatHistory.Add(new ApplicationStatHistory { ApplicationId = _context.Applications.Last().ApplicationId
                    , ApplicationStat = _context.ApplicationStats.FirstOrDefault(s => s.Id == 1).Name, DateOfUpdate = DateTime.Now });
                _context.SaveChanges();
                return RedirectToAction("Details", "Applications", new { id = application.ApplicationId });
            }

            ViewData["BilateralProtocol1Id"] = new SelectList(_context.BilateralProtocols.Where(p => p.SubjectId == student.CollegeSubjectId), "Id", "Destination");
            ViewData["BilateralProtocol2Id"] = new SelectList(_context.BilateralProtocols.Where(p => p.SubjectId == student.CollegeSubjectId), "Id", "Destination");
            ViewData["BilateralProtocol3Id"] = new SelectList(_context.BilateralProtocols.Where(p => p.SubjectId == student.CollegeSubjectId), "Id", "Destination");
            ViewData["ApplicationStatId"] = 1;
            ViewData["EmployeeId"] = "";
            ViewData["CreationDate"] = DateTime.Now;
            loadHelp();

            return RedirectToAction("Create", new { userId = application.StudentId });
        }
        ///<summary>
        ///The objective of this method is to process all the students who applied to the outgoing program with their respective grade.
        ///The processed students are those who had an application in the "Pending serialization" state.
        ///All the applications final grade is calculated in the moment of the seriation based on the the grades stored in each application.
        ///</summary>
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Seriation()
        {
            DateTime openDate = _context.Editals.Last().OpenDate;
            DateTime closeDate = _context.Editals.Last().CloseDate;

            var queryGetApplication = await _context.Applications.Include(a => a.ApplicationStat).Include(a => a.Student).Include(a => a.Student.CollegeSubject).Include(a => a.Student.CollegeSubject.College).Include(a => a.BilateralProtocol1).Include(a => a.BilateralProtocol2).Include(a => a.BilateralProtocol3).Where(a => a.ApplicationStatId == 3 && a.CreationDate >= openDate && a.CreationDate <= closeDate).ToListAsync();
            var queryGetAllApplication = await _context.Applications.Where(a => a.CreationDate >= openDate && a.CreationDate <= closeDate).ToListAsync();
            if (queryGetApplication.Count() != queryGetAllApplication.Count())
            {
                return RedirectToAction("Applications", "Home", new { message = "Ainda existem candidaturas por avaliar" });
            }

            foreach (var item in queryGetApplication)
            {
                item.FinalGrade = (item.MotivationLetter + item.Interview + item.ArithmeticMean) / 3;
                await _context.SaveChangesAsync();
            }

            var OrderedList = queryGetApplication.OrderByDescending(q => q.FinalGrade).ToList();
            foreach (var item in OrderedList)
            {
                string studentEmail = _context.Students.Where(s => s.Id.Equals(item.StudentId)).Select(s => s.Email).FirstOrDefault();
                if (item.BilateralProtocol1.OpenSlots > 0 && item.FinalGrade >= 9.5)
                {
                    String appStat = _context.ApplicationStats.SingleOrDefault(a => a.Id == item.ApplicationStatId).Name;
                    _context.ApplicationStatHistory.Add(new ApplicationStatHistory { ApplicationId = item.ApplicationId, ApplicationStat = appStat, DateOfUpdate = DateTime.Now });
                    item.ApplicationStatId = 4;
                    item.ApplicationStat = _context.ApplicationStats.SingleOrDefault(a => a.Id == 4);
                    item.BilateralProtocol1.OpenSlots -= 1;
                    item.BilateralProtocol2 = null;
                    item.BilateralProtocol3 = null;
                    await _context.SaveChangesAsync();
                }
                else if (item.BilateralProtocol2 != null && item.BilateralProtocol2.OpenSlots > 0 && item.FinalGrade >= 9.5)
                {
                    String appStat = _context.ApplicationStats.SingleOrDefault(a => a.Id == item.ApplicationStatId).Name;
                    _context.ApplicationStatHistory.Add(new ApplicationStatHistory { ApplicationId = item.ApplicationId, ApplicationStat = appStat, DateOfUpdate = DateTime.Now });
                    item.ApplicationStatId = 4;
                    item.ApplicationStat = _context.ApplicationStats.SingleOrDefault(a => a.Id == 4);
                    item.BilateralProtocol2.OpenSlots -= 1;
                    item.BilateralProtocol1 = null;
                    item.BilateralProtocol3 = null;
                    await _context.SaveChangesAsync();

                }
                else if (item.BilateralProtocol3 != null && item.BilateralProtocol3.OpenSlots > 0 && item.FinalGrade >= 9.5)
                {
                    String appStat = _context.ApplicationStats.SingleOrDefault(a => a.Id == item.ApplicationStatId).Name;
                    _context.ApplicationStatHistory.Add(new ApplicationStatHistory { ApplicationId = item.ApplicationId, ApplicationStat = appStat, DateOfUpdate = DateTime.Now });
                    item.ApplicationStatId = 4;
                    item.ApplicationStat = _context.ApplicationStats.SingleOrDefault(a => a.Id == 4);
                    item.BilateralProtocol3.OpenSlots -= 1;
                    item.BilateralProtocol1 = null;
                    item.BilateralProtocol2 = null;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    String appStat = _context.ApplicationStats.SingleOrDefault(a => a.Id == item.ApplicationStatId).Name;
                    _context.ApplicationStatHistory.Add(new ApplicationStatHistory { ApplicationId = item.ApplicationId, ApplicationStat = appStat, DateOfUpdate = DateTime.Now });
                    item.ApplicationStatId = 5;
                    item.ApplicationStat = _context.ApplicationStats.SingleOrDefault(a => a.Id == 5);
                    await _context.SaveChangesAsync();
                }
                emailSender.SendStateEmail(item.ApplicationStatId, studentEmail);
            }
            //->NEEDS TO BE CHECKED<-
            //publishSeriationNews();
            return RedirectToAction("DisplaySeriation", "Applications");
        }

        /// <summary>
        /// Publishes a News with the new seriation link so the users can access it.
        /// </summary>
        private void publishSeriationNews()
        {
            Edital latestEdital = _context.Editals.OrderByDescending(e => e.Id).FirstOrDefault();
            string title = "Seriação " + latestEdital.CloseDate.Year;
            string content = "Encontra-se disponivel a seriação dos alunos respetiva do ultimo edital";
            News news = new News()
            {
                EmployeeId = this.User.FindFirstValue(ClaimTypes.NameIdentifier),
                IsPublished = true,
                Title = title,
                TextContent = content
            };
            Document urlDoc = new Document
            {
                EmployeeId = news.EmployeeId,
                Description = "Documento de " + news.Title,
                FileUrl = "Seriacoes",
                UploadDate = DateTime.Now
            };
            _context.Add(urlDoc);
            news.Document = urlDoc;
            _context.Add(news);
            _context.SaveChanges();
        }

        ///<summary>
        ///This method displays the results of the seriation.
        ///All the stundentds will be displayed with their respective grade
        ///</summary>
        public async Task<IActionResult> DisplaySeriation()
        {
            DateTime openDate = _context.Editals.Last().OpenDate;
            DateTime closeDate = _context.Editals.Last().CloseDate;
            var seriation = await _context.Applications.Include(a => a.ApplicationStat).Include(a => a.Student).Include(a => a.Student.CollegeSubject).Include(a => a.Student.CollegeSubject.College).Include(a => a.BilateralProtocol1).Include(a => a.BilateralProtocol2).Include(a => a.BilateralProtocol3).Where(a => a.CreationDate >= openDate && a.CreationDate <= closeDate)
                .Where(a => a.ApplicationStatId == 4 || a.ApplicationStatId == 5).OrderByDescending(q => q.FinalGrade).ToListAsync();

            return View(seriation);
        }

        ///<summary>
        ///This method displays the various stages that the application had over the time.
        ///This only displays the history of the most recent application.
        ///</summary>
        ///<param name="studentId">A String that represents the id of the current student which wants to check his application history.</param>
        public async Task<IActionResult> ApplicationHistory(String studentId)
        {
            if (_context.Applications.Include(a => a.Student).Where(a => a.StudentId.Equals(studentId)).Count() == 0)
            {
                return RedirectToAction("Application", "Home", new { message = "Não possui uma candidatura" });
            }
            int getCurrentApplication = _context.Applications.Include(a => a.Student).Where(a => a.StudentId.Equals(studentId)).Last().ApplicationId;
            var getCurrentApplicationHistory = _context.ApplicationStatHistory.Where(a => a.ApplicationId == getCurrentApplication);
            return View(await getCurrentApplicationHistory.ToListAsync());
        }


        ///<summary>
        ///The objective of this method is filter all applications by the ones the current logged in employee is evaluating or the ones that currently aren't being evaluated.
        ///</summary>
        /// <param name="filterType">A String that represents the filter that will be applyed to the application list.</param>
        /// <param name="employeeId">A String that represents the id of the employee who is currently checking the application list.</param>
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Filter(String filterType, String employeeId)
        {
            DateTime openDate = _context.Editals.Last().OpenDate;
            DateTime closeDate = _context.Editals.Last().CloseDate;
            var allApplications = _context.Applications.Include(a => a.ApplicationStat).Include(a => a.Employee).Include(a => a.Student).Where(a => a.CreationDate >= openDate && a.CreationDate <= closeDate);

            if (filterType.Equals("CurrentlySupervising"))
            {
                var filteredApplications = allApplications.Where(a => a.EmployeeId.Equals(employeeId));

                if (_context.Editals.Count() > 1)
                {
                    DateTime lastOpenDate = _context.Editals.OrderByDescending(e => e.CloseDate).Skip(1).Take(1).FirstOrDefault().OpenDate;
                    DateTime lastCloseDate = _context.Editals.OrderByDescending(e => e.CloseDate).Skip(1).Take(1).FirstOrDefault().CloseDate;
                    var lastApplications = _context.Applications.Include(a => a.ApplicationStat)
                        .Include(a => a.Employee).Include(a => a.Student).Include(a => a.BilateralProtocol1)
                        .Include(a => a.BilateralProtocol2).Include(a => a.BilateralProtocol3)
                        .Where(a => a.CreationDate >= lastOpenDate && a.CreationDate <= lastCloseDate);
                    var lastFilteredApplications = lastApplications.Where(a => a.EmployeeId.Equals(employeeId));
                    ViewData["lastApplications"] = lastFilteredApplications;
                }

                return View(await filteredApplications.ToListAsync());
            }
            if (filterType.Equals("NotSupervising"))
            {
                var filteredApplications = allApplications.Where(a => a.EmployeeId.Equals(null));

                if (_context.Editals.Count() > 1)
                {
                    DateTime lastOpenDate = _context.Editals.OrderByDescending(e => e.CloseDate).Skip(1).Take(1).FirstOrDefault().OpenDate;
                    DateTime lastCloseDate = _context.Editals.OrderByDescending(e => e.CloseDate).Skip(1).Take(1).FirstOrDefault().CloseDate;
                    var lastApplications = _context.Applications.Include(a => a.ApplicationStat)
                        .Include(a => a.Employee).Include(a => a.Student).Include(a => a.BilateralProtocol1)
                        .Include(a => a.BilateralProtocol2).Include(a => a.BilateralProtocol3)
                        .Where(a => a.CreationDate >= lastOpenDate && a.CreationDate <= lastCloseDate);
                    var lastFilteredApplications = lastApplications.Where(a => a.EmployeeId.Equals(null));
                    ViewData["lastApplications"] = lastFilteredApplications;
                }

                return View(await filteredApplications.ToListAsync());
            }
            else
            {
                RedirectToAction("Index", "Applications", new { employeeId = employeeId });
            }

            return View(await allApplications.ToListAsync());
        }
        /// <summary>
        /// This method sends the current employee to a verification screen to make sure the employee wants to finalize the application.
        /// </summary>
        /// <param name="studentId">A String that represents the stundent who has the application that the employee wants to finalize.</param>
        /// <returns></returns>
        public async Task<IActionResult> CloseApplication(String studentId)
        {
            var getCurrentApplication = await _context.Applications.Include(a => a.Student).LastOrDefaultAsync(a => a.StudentId.Equals(studentId));

            return View(getCurrentApplication);
        }
        /// <summary>
        /// This method finalizes the application and concludes the outgoing process of the application.
        /// </summary>
        /// <param name="applicationId">An int that represents the id of the application that will be closed.</param>
        /// <param name="employeeId">A String that represents the employee who has the application that the employee wants to finalize.</param>
        /// <returns></returns>
        public async Task<IActionResult> FinishApplication(int applicationId, String employeeId)
        {

            var changedApplication = await _context.Applications.Include(a => a.Student).Include(a => a.ApplicationStat).SingleOrDefaultAsync(a => a.ApplicationId == applicationId);
            if (changedApplication.ApplicationStatId != 4)
            {
                return RedirectToAction("Index", "Applications", new { employeeId = changedApplication.EmployeeId });
            }
            _context.ApplicationStatHistory.Add(new ApplicationStatHistory { ApplicationId = changedApplication.ApplicationId, ApplicationStat = changedApplication.ApplicationStat.Name, DateOfUpdate = DateTime.Now });
            _context.SaveChanges();
            _context.Entry(changedApplication).State = EntityState.Detached;
            changedApplication = await _context.Applications.Include(a => a.Student).SingleOrDefaultAsync(a => a.ApplicationId == applicationId);
            changedApplication.ApplicationStatId = 6;
            _context.SaveChanges();

            return RedirectToAction("Index", "Applications", new { employeeId = employeeId });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications.Include(s => s.Student).SingleOrDefaultAsync(m => m.ApplicationId == id);
            if (application == null)
            {
                return NotFound();
            }

            ViewData["BilateralProtocol1Id"] = application.BilateralProtocol1Id;
            ViewData["BilateralProtocol2Id"] = application.BilateralProtocol2Id;
            ViewData["BilateralProtocol3Id"] = application.BilateralProtocol3Id;
            ViewData["ApplicationStatId"] = new SelectList(_context.ApplicationStats.Where(a => a.Id != 1 && a.Id < 4), "Id", "Name", application.ApplicationStatId);
            ViewData["EmployeeId"] = application.EmployeeId;
            ViewData["StudentId"] = application.StudentId;
            ViewData["CreationDate"] = application.CreationDate;
            ViewData["Motivations"] = application.Motivations;
            loadHelp();

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApplicationId,StudentId,ApplicationStatId,EmployeeId,BilateralProtocol1Id,BilateralProtocol2Id,BilateralProtocol3Id,CreationDate,ArithmeticMean,ECTS,MotivationLetter,Interview,FinalGrade,Motivations")] Application application)
        {
            if (id != application.ApplicationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var getPreviousStat = _context.Applications.Include(a => a.ApplicationStat).SingleOrDefault(a => a.ApplicationId == id);
                    if (getPreviousStat.ApplicationStatId != application.ApplicationStatId)
                    {
                        var student = _context.Students.Where(u => u.Id.Equals(application.StudentId)).FirstOrDefault();
                        string newStat = _context.ApplicationStats.Where(a => a.Id == application.ApplicationStatId).Select(a => a.Name).FirstOrDefault();
                        _context.ApplicationStatHistory.Add(new ApplicationStatHistory { ApplicationId = id, ApplicationStat = getPreviousStat.ApplicationStat.Name, DateOfUpdate = DateTime.Now });
                        emailSender.SendStateEmail(application.ApplicationStatId, student.Email);
                    }
                    _context.Entry(getPreviousStat).State = EntityState.Detached;
                    _context.Update(application);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationExists(application.ApplicationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Applications", new { employeeId = application.EmployeeId });
            }
            ViewData["BilateralProtocol1Id"] = application.BilateralProtocol1Id;
            ViewData["BilateralProtocol2Id"] = application.BilateralProtocol2Id;
            ViewData["BilateralProtocol3Id"] = application.BilateralProtocol3Id;
            ViewData["EmployeeId"] = application.EmployeeId;
            ViewData["StudentId"] = application.StudentId;
            ViewData["CreationDate"] = application.CreationDate;
            return RedirectToAction("Index", "Applications", new { employeeId = application.EmployeeId });
        }

        // GET: Applications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.ApplicationStat)
                .Include(a => a.Employee)
                .Include(a => a.Student)
                .SingleOrDefaultAsync(m => m.ApplicationId == id);
            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // POST: Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.Applications.SingleOrDefaultAsync(m => m.ApplicationId == id);
            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ///<summary>
        ///This action will load a view with a date picker so the employee can pick a date for an interview with a student.
        ///<param name="id">Application Id.</param>
        ///</summary>
        public IActionResult ScheduleInterview(int id)
        {
            ViewData["ApplicationId"] = id;
            loadHelp();
            return View(_context.Applications.Include(a => a.Student).Where(a => a.ApplicationId == id).SingleOrDefault());
        }

        ///<summary>
        ///This method will send an email to the student witth the date of his interview, selected by the employee
        ///<param name="studentId">Selected student Id.</param>
        ///<param name="employeeID">Logged in employee Id.</param>
        ///<param name="interviewDate">Desired interview date.</param>
        ///</summary>
        public async Task<IActionResult> EmailScheduleInterview(string studentId, string employeeID, DateTime interviewDate)
        {
            Student user = _context.Students.Where(s => s.Id.Equals(studentId)).FirstOrDefault();
            await emailSender.Execute("Entrevista Agendada", "Saudações, " +
                user.UserFullname + " uma entrevista consigo foi agendada para o dia " + interviewDate +
                " no nosso gabinete. Entre em contacto conosco se não for possivel comparecer a esta entrevista." +
                " Uma falta sem justificação irá resultar numa avaliação de 0.", user.Email);
            return RedirectToAction("Index", "Applications", new { employeeId = employeeID });
        }

        //Loads help tips
        private void loadHelp()
        {
            ViewData["BilateralTip"] = (_context.Helps.FirstOrDefault(h => h.HelpName == "Bilateral") as Help).HelpDescription;
            ViewData["MotivationTip"] = (_context.Helps.FirstOrDefault(h => h.HelpName == "MotivationLetter") as Help).HelpDescription;
            ViewData["GradeTip"] = (_context.Helps.FirstOrDefault(h => h.HelpName == "Grade") as Help).HelpDescription;
            ViewData["MotivationGradeTip"] = (_context.Helps.FirstOrDefault(h => h.HelpName == "MotivationGrade") as Help).HelpDescription;
            ViewData["InterviewTip"] = (_context.Helps.FirstOrDefault(h => h.HelpName == "Interview") as Help).HelpDescription;
            ViewData["InterviewDateTip"] = (_context.Helps.FirstOrDefault(h => h.HelpName == "InterviewDate") as Help).HelpDescription;
        }

        //verifies if an application with a given id exists
        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.ApplicationId == id);
        }
    }
}
