using _25DH110384_WebStore.Models;
using _25DH110384_WebStore.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace _25DH110384_WebStore.Controllers
{
    public class AccountController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem tên đăng nhập đã tồn tại chưa
                var existingUser = db.Users.SingleOrDefault(u => u.Username == model.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại!");
                    return View(model);
                }

                // Tạo bản ghi User
                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password,
                    UserRole = "Customer"
                };
                db.Users.Add(user);

                // Tạo bản ghi Customer
                var customer = new Customer
                {
                    CustomerName = model.CustomerName,
                    CustomerEmail = model.CustomerEmail,
                    CustomerPhone = model.CustomerPhone,
                    CustomerAddress = model.CustomerAddress,
                    Username = model.Username,
                };
                db.Customers.Add(customer);
                db.SaveChanges();

                // Tự động đăng nhập sau khi đăng ký xong
                Session["Username"] = user.Username;
                Session["UserRole"] = user.UserRole;
                FormsAuthentication.SetAuthCookie(user.Username, false);

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.SingleOrDefault(u =>
                    u.Username == model.Username &&
                    u.Password == model.Password &&
                    u.UserRole == "Customer"
                );

                if (user != null)
                {
                    // Lưu Session
                    Session["Username"] = user.Username;
                    Session["UserRole"] = user.UserRole;

                    // Lưu Cookie xác thực
                    FormsAuthentication.SetAuthCookie(user.Username, false);

                    // Chuyển về trang chủ Home
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                }
            }

            return View(model);
        }

        // GET: Account/ProfileInfo (Hiển thị trang Thông tin tài khoản)
        public ActionResult ProfileInfo()
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string username = Session["Username"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new ProfileVM
            {
                CustomerID = customer.CustomerID,
                Username = customer.Username,
                CustomerName = customer.CustomerName,
                CustomerPhone = customer.CustomerPhone,
                CustomerEmail = customer.CustomerEmail,
                CustomerAddress = customer.CustomerAddress
            };

            return View(model);
        }

        // POST: Account/ProfileInfo (Cập nhật thông tin cá nhân)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProfileInfo(ProfileVM model)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var customer = db.Customers.Find(model.CustomerID);
                if (customer != null)
                {
                    customer.CustomerName = model.CustomerName;
                    customer.CustomerPhone = model.CustomerPhone;
                    customer.CustomerEmail = model.CustomerEmail;
                    customer.CustomerAddress = model.CustomerAddress;

                    db.SaveChanges();
                    ViewBag.Message = "Cập nhật thông tin thành công!";
                }
            }

            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }
    }
}