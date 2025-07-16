import React from "react";
import "./CommonFooter.scss";

const CommonFooter = () => {
  return (
    <footer className="common-footer">
      <div className="container">
        <div className="row">
          {/* Thông tin công ty */}
          <div className="col-lg-4 col-md-6 col-sm-12 mb-4">
            <div className="footer-section">
              <h5 className="footer-title">B2P Platform</h5>
              <p className="footer-description">
                Nền tảng đặt sân thể thao trực tuyến, kết nối đam mê thể thao
                với những sân chơi chất lượng nhất.
              </p>
              <div className="social-links">
                <a href="#" className="social-link" aria-label="Facebook">
                  <i className="fab fa-facebook-f"></i>
                </a>
                <a href="#" className="social-link" aria-label="Twitter">
                  <i className="fab fa-twitter"></i>
                </a>
                <a href="#" className="social-link" aria-label="LinkedIn">
                  <i className="fab fa-linkedin-in"></i>
                </a>
                <a href="#" className="social-link" aria-label="Instagram">
                  <i className="fab fa-instagram"></i>
                </a>
              </div>
            </div>
          </div>

          {/* Chính sách */}
          <div className="col-lg-2 col-md-6 col-sm-6 mb-4">
            <div className="footer-section">
              <h6 className="footer-subtitle">Chính sách</h6>
              <ul className="footer-links">
                <li>
                  <a href="/privacy-policy">Chính sách bảo mật</a>
                </li>
                <li>
                  <a href="/terms-of-service">Điều khoản sử dụng</a>
                </li>
                <li>
                  <a href="/cookie-policy">Chính sách Cookie</a>
                </li>
                <li>
                  <a href="/refund-policy">Chính sách hoàn tiền</a>
                </li>
              </ul>
            </div>
          </div>

          {/* Hỗ trợ */}
          <div className="col-lg-2 col-md-6 col-sm-6 mb-4">
            <div className="footer-section">
              <h6 className="footer-subtitle">Hỗ trợ</h6>
              <ul className="footer-links">
                <li>
                  <a href="/help-center">Trung tâm trợ giúp</a>
                </li>
                <li>
                  <a href="/faq">Câu hỏi thường gặp</a>
                </li>
                <li>
                  <a href="/contact">Liên hệ hỗ trợ</a>
                </li>
                <li>
                  <a href="/feedback">Góp ý</a>
                </li>
                <li>
                  <a href="/report-bug">Báo lỗi</a>
                </li>
              </ul>
            </div>
          </div>

          {/* Thông tin liên hệ */}
          <div className="col-lg-4 col-md-6 col-sm-12 mb-4">
            <div className="footer-section">
              <h6 className="footer-subtitle">Thông tin liên hệ</h6>
              <div className="contact-info">
                <div className="contact-item">
                  <i className="fas fa-map-marker-alt"></i>
                  <span>123 Đường ABC, Quận XYZ, TP. Hồ Chí Minh</span>
                </div>
                <div className="contact-item">
                  <i className="fas fa-phone"></i>
                  <span>+84 123 456 789</span>
                </div>
                <div className="contact-item">
                  <i className="fas fa-envelope"></i>
                  <span>contact@b2p.com</span>
                </div>
                <div className="contact-item">
                  <i className="fas fa-clock"></i>
                  <span>Thứ 2 - Chủ Nhật: 7:00 - 20:00</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Copyright */}
        <div className="footer-bottom">
          <div className="row align-items-center">
            <div className="col-md-6">
              <p className="copyright">
                © 2025 B2P Platform. All rights reserved.
              </p>
            </div>
            <div className="col-md-6">
              <div className="footer-bottom-links">
                <a href="/sitemap">Sitemap</a>
                <span className="separator">|</span>
                <a href="/accessibility">Accessibility</a>
                <span className="separator">|</span>
                <a href="/careers">Careers</a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default CommonFooter;
