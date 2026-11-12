using _25DH110384_WebStore.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Areas.Admin.Controllers
{
    public class CustomersController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // GET: Admin/Customers
        // Hiển thị danh sách tất cả khách hàng
        public ActionResult Index()
        {
            return View(db.Customers.ToList());
        }

        // GET: Admin/Customers/Details/5
        // Xem thông tin chi tiết của 1 khách hàng
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Nạp Customer kèm theo danh sách Orders -> OrderDetails -> Product
            var customer = db.Customers
                             .Include(c => c.Orders)
                             .Include(c => c.Orders.Select(o => o.OrderDetails.Select(od => od.Product)))
                             .SingleOrDefault(c => c.CustomerID == id);

            if (customer == null)
            {
                return HttpNotFound();
            }

            // Sắp xếp các đơn hàng mới nhất lên đầu
            if (customer.Orders != null)
            {
                customer.Orders = customer.Orders.OrderByDescending(o => o.OrderDate).ToList();
            }

            return View(customer);
        }

        // POST: Admin/Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var customer = db.Customers.Find(id);
            if (customer != null)
            {
                // 1. Tìm tất cả đơn hàng của Khách hàng này
                var customerOrders = db.Orders.Where(o => o.CustomerID == id).ToList();

                if (customerOrders.Any())
                {
                    foreach (var order in customerOrders)
                    {
                        // 2. Xóa các Chi tiết đơn hàng (OrderDetails) của từng đơn
                        var orderDetails = db.OrderDetails.Where(od => od.OrderID == order.OrderID).ToList();
                        if (orderDetails.Any())
                        {
                            db.OrderDetails.RemoveRange(orderDetails);
                        }
                    }
                    // 3. Xóa các Đơn hàng (Orders) của khách hàng
                    db.Orders.RemoveRange(customerOrders);
                }

                // 4. Tìm và xóa luôn tài khoản User đăng nhập liên kết (nếu có)
                var user = db.Users.SingleOrDefault(u => u.Username == customer.Username);
                if (user != null)
                {
                    db.Users.Remove(user);
                }

                // 5. Cuối cùng mới xóa thông tin Khách hàng (Customer)
                db.Customers.Remove(customer);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}