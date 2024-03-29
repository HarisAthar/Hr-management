﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mondayWebApp.Data;
using mondayWebApp.Models;

namespace mondayWebApp.Controllers
{
    [Authorize(Roles = "Superadmin, Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Departments.Include(d => d.DepartmentManager);
            return View(await applicationDbContext.ToListAsync());
        }
       


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.DepartmentManager)
                .SingleOrDefaultAsync(m => m.DepartmentID == id);

            if (department == null) return NotFound();

            return View(department);
        }

        public IActionResult Create()
        {
            ViewData["DepartmentManagerID"] = GetAdminList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentID,DepartmentName,DepartmentDesc,DepartmentEstablishmentDate,DepartmentManagerID,IsEdited,IsChecked")] Department department)
        {
            if (ModelState.IsValid)
            {
                if (_context.Departments.Any(d => d.DepartmentManagerID == department.DepartmentManagerID))
                {
                    ModelState.AddModelError("DepartmentManagerID", "This manager is already assigned to a department.");
                    ViewData["DepartmentManagerID"] = GetAdminList();
                    return View(department);
                }

                _context.Add(department);
                await _context.SaveChangesAsync();

                var employeeManager = _context.Employees
                    .Where(e => e.DepartmentManager.DepartmentManagerID == department.DepartmentManagerID)
                    .Single();

                employeeManager.IsDepartmentManager = true;
                employeeManager.DepartmentID = department.DepartmentID;
                _context.Employees.Update(employeeManager);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentManagerID"] = GetAdminList();
            return View(department);
        }

        private SelectList GetAdminList()
        {
            return new SelectList(_context.Employees.Where(e => e.EmployeeRole == "Admin"), "EmployeeID", "EmployeeNameSurname");
        }

        private static int? TempBox { get; set; }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.FindAsync(id);
            TempBox = department.DepartmentManagerID;

            if (department == null) return NotFound();

            ViewData["DepartmentManagerID"] = GetAdminList();
            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmentID,DepartmentName,DepartmentDesc,DepartmentEstablishmentDate,DepartmentManagerID,IsEdited,IsChecked")] Department department)
        {
            if (id != department.DepartmentID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (department.DepartmentManagerID != null)
                    {
                        if (TempBox == null)
                        {
                            _context.Update(department);
                            await _context.SaveChangesAsync();

                            var employeeManager = _context.Employees
                                .Where(e => e.DepartmentManager.DepartmentManagerID == department.DepartmentManagerID)
                                .Single();

                            employeeManager.IsDepartmentManager = true;
                            employeeManager.DepartmentID = department.DepartmentID;
                            _context.Employees.Update(employeeManager);
                            await _context.SaveChangesAsync();

                            return RedirectToAction(nameof(Index));
                        }
                        else if (TempBox != null)
                        {
                            var oldEmployeeManager = _context.Employees
                                .Where(e => e.EmployeeID == TempBox)
                                .Single();

                            oldEmployeeManager.IsDepartmentManager = false;
                            _context.Employees.Update(oldEmployeeManager);
                            await _context.SaveChangesAsync();

                            _context.Update(department);
                            await _context.SaveChangesAsync();

                            var employeeManager = _context.Employees
                                .Where(e => e.DepartmentManager.DepartmentManagerID == department.DepartmentManagerID)
                                .Single();

                            employeeManager.IsDepartmentManager = true;
                            employeeManager.DepartmentID = department.DepartmentID;
                            _context.Employees.Update(employeeManager);
                            await _context.SaveChangesAsync();

                            return RedirectToAction(nameof(Index));
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DepartmentID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentManagerID"] = GetAdminList();
            return View(department);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.DepartmentManager)
                .SingleOrDefaultAsync(m => m.DepartmentID == id);

            if (department == null) return NotFound();

            return View(department);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            var employeeManager = _context.Employees
                .Where(e => e.DepartmentManager.DepartmentManagerID == department.DepartmentManagerID)
                .Single();

            employeeManager.IsDepartmentManager = false;
            _context.Employees.Update(employeeManager);
            await _context.SaveChangesAsync();

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DepartmentID == id);
        }
    }
}
