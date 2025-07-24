import React from "react";
import "./CommonFooter.scss";

const CommonFooter = () => {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="common-footer">
      <div className="container">
        <div className="row">
          {/* Thông tin công ty */}
          <div className="col-lg-4 col-md-6 col-sm-12 mb-4">
            <div className="footer-section">
              <h5 className="footer-title">
                <i className="fas fa-futbol me-2"></i>
                B2P Platform
              </h5>
              <p className="footer-description">
                Nền tảng đặt sân thể thao trực tuyến, kết nối đam mê thể thao
                với những sân chơi chất lượng nhất.
              </p>
              <div className="social-links">
                <h6 className="social-title">Theo dõi chúng tôi</h6>
                <div className="social-icons">
                  <a
                    href="#"
                    className="social-link facebook"
                    aria-label="Facebook"
                  >
                    <i className="fab fa-facebook-f"></i>
                  </a>
                  <a
                    href="#"
                    className="social-link twitter"
                    aria-label="Twitter"
                  >
                    <i className="fab fa-twitter"></i>
                  </a>
                  <a
                    href="#"
                    className="social-link linkedin"
                    aria-label="LinkedIn"
                  >
                    <i className="fab fa-linkedin-in"></i>
                  </a>
                  <a
                    href="#"
                    className="social-link instagram"
                    aria-label="Instagram"
                  >
                    <i className="fab fa-instagram"></i>
                  </a>
                  <a
                    href="#"
                    className="social-link youtube"
                    aria-label="YouTube"
                  >
                    <i className="fab fa-youtube"></i>
                  </a>
                </div>
              </div>
            </div>
          </div>

          {/* Chính sách */}
          <div className="col-lg-2 col-md-6 col-sm-6 mb-4">
            <div className="footer-section">
              <h6 className="footer-subtitle">Chính sách</h6>
              <ul className="footer-links">
                <li>
                  <a href="/privacy-policy">
                    <i className="fas fa-shield-alt me-2"></i>
                    Chính sách bảo mật
                  </a>
                </li>
                <li>
                  <a href="/terms-of-service">
                    <i className="fas fa-file-contract me-2"></i>
                    Điều khoản sử dụng
                  </a>
                </li>
                <li>
                  <a href="/cookie-policy">
                    <i className="fas fa-cookie-bite me-2"></i>
                    Chính sách Cookie
                  </a>
                </li>
                <li>
                  <a href="/refund-policy">
                    <i className="fas fa-undo-alt me-2"></i>
                    Chính sách hoàn tiền
                  </a>
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
                  <a href="/help-center">
                    <i className="fas fa-question-circle me-2"></i>
                    Trung tâm trợ giúp
                  </a>
                </li>
                <li>
                  <a href="/faq">
                    <i className="fas fa-comments me-2"></i>
                    Câu hỏi thường gặp
                  </a>
                </li>
                <li>
                  <a href="/contact">
                    <i className="fas fa-headset me-2"></i>
                    Liên hệ hỗ trợ
                  </a>
                </li>
                <li>
                  <a href="/feedback">
                    <i className="fas fa-comment-dots me-2"></i>
                    Góp ý
                  </a>
                </li>
                <li>
                  <a href="/report-bug">
                    <i className="fas fa-bug me-2"></i>
                    Báo lỗi
                  </a>
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
                  <div className="contact-text">
                    <span className="contact-label">Địa chỉ:</span>
                    <span>123 Đường ABC, Quận XYZ, TP. Hồ Chí Minh</span>
                  </div>
                </div>
                <div className="contact-item">
                  <i className="fas fa-phone"></i>
                  <div className="contact-text">
                    <span className="contact-label">Hotline:</span>
                    <a href="tel:+84123456789">+84 123 456 789</a>
                  </div>
                </div>
                <div className="contact-item">
                  <i className="fas fa-envelope"></i>
                  <div className="contact-text">
                    <span className="contact-label">Email:</span>
                    <a href="mailto:contact@b2p.com">contact@b2p.com</a>
                  </div>
                </div>
                <div className="contact-item">
                  <i className="fas fa-clock"></i>
                  <div className="contact-text">
                    <span className="contact-label">Giờ làm việc:</span>
                    <span>Thứ 2 - Chủ Nhật: 7:00 - 20:00</span>
                  </div>
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
                © {currentYear} B2P Platform. All rights reserved.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Decorative Elements */}
      <div className="footer-decorations">
        <div className="decoration decoration-1"></div>
        <div className="decoration decoration-2"></div>
        <div className="decoration decoration-3"></div>
      </div>
    </footer>
  );
};

export default CommonFooter;
