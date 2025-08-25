import React, { useState } from 'react';
import { ChevronRight, Key, CreditCard, Shield, CheckCircle, ExternalLink, Copy, Eye, EyeOff } from 'lucide-react';

export default function PaymentMerchantGuide() {
  const [activeTab, setActiveTab] = useState('zalopay');
  const [copiedKey, setCopiedKey] = useState('');
  const [showApiKey, setShowApiKey] = useState(false);

  const copyToClipboard = (text, keyType) => {
    navigator.clipboard.writeText(text);
    setCopiedKey(keyType);
    setTimeout(() => setCopiedKey(''), 2000);
  };

  const stepData = [
    {
      title: "Truy cập trang đăng ký",
      desc: "Vào trang merchant.zalopay.vn để bắt đầu đăng ký",
      action: "Truy cập ngay",
      link: "https://merchant.zalopay.vn"
    },
    {
      title: "Điền thông tin doanh nghiệp",
      desc: "Cung cấp thông tin về công ty, mã số thuế, giấy phép kinh doanh"
    },
    {
      title: "Upload tài liệu",
      desc: "Tải lên các giấy tờ pháp lý: GPKD, CMT/CCCD người đại diện"
    },
    {
      title: "Chờ duyệt và nhận key",
      desc: "ZaloPay sẽ gửi App ID và Key qua email sau khi duyệt"
    }
  ];

  const stripeStepData = [
    {
      title: "Nhấn vào link kết nối",
      desc: "Sử dụng link Connect được cung cấp bởi platform để bắt đầu quá trình kết nối",
      action: "Create Connect Account",
      link: "https://connect.stripe.com/d/setup/c/_SvsASj4EVN4lpB5ooRbrvngwg3/YWNjdF8xUzAwUDVBWWZJVGtKSVBG/c5bdc89e5fa04d6b2"
    },
    {
      title: "Đăng nhập hoặc tạo tài khoản Stripe",
      desc: "Nếu chưa có tài khoản Stripe, bạn sẽ được hướng dẫn tạo tài khoản mới"
    },
    {
      title: "Xác nhận quyền truy cập",
      desc: "Cho phép platform truy cập tài khoản Stripe của bạn để xử lý payments"
    },
    {
      title: "Hoàn tất KYC và thông tin thanh toán",
      desc: "Cung cấp thông tin doanh nghiệp và tài khoản ngân hàng để nhận tiền"
    }
  ];

  const styles = {
    container: {
      minHeight: '100vh',
      background: 'linear-gradient(135deg, #f3e8ff 0%, #dbeafe 50%, #e0e7ff 100%)'
    },
    header: {
      background: 'white',
      boxShadow: '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
      borderBottom: '4px solid #3b82f6'
    },
    headerContent: {
      maxWidth: '1280px',
      margin: '0 auto',
      padding: '32px 24px'
    },
    headerCenter: {
      textAlign: 'center'
    },
    headerIconContainer: {
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      gap: '16px',
      marginBottom: '16px'
    },
    headerIcon: {
      padding: '12px',
      background: 'linear-gradient(to right, #3b82f6, #7c3aed)',
      borderRadius: '50%'
    },
    title: {
      fontSize: '36px',
      fontWeight: 'bold',
      background: 'linear-gradient(to right, #2563eb, #7c3aed)',
      WebkitBackgroundClip: 'text',
      backgroundClip: 'text',
      color: 'transparent',
      margin: 0
    },
    subtitle: {
      fontSize: '20px',
      color: '#4b5563',
      maxWidth: '768px',
      margin: '0 auto'
    },
    main: {
      maxWidth: '1280px',
      margin: '0 auto',
      padding: '48px 24px'
    },
    tabContainer: {
      display: 'flex',
      justifyContent: 'center',
      marginBottom: '48px'
    },
    tabWrapper: {
      background: 'white',
      borderRadius: '16px',
      padding: '8px',
      boxShadow: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
      border: '1px solid #e5e7eb',
      display: 'flex'
    },
    tabButton: {
      padding: '16px 32px',
      borderRadius: '12px',
      fontWeight: '600',
      transition: 'all 0.3s ease',
      display: 'flex',
      alignItems: 'center',
      gap: '12px',
      border: 'none',
      cursor: 'pointer',
      fontSize: '16px'
    },
    tabButtonActive: {
      boxShadow: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
      transform: 'scale(1.05)',
      color: 'white'
    },
    tabButtonInactive: {
      color: '#4b5563',
      background: 'transparent'
    },
    zalopayTab: {
      background: 'linear-gradient(to right, #f97316, #ef4444)'
    },
    stripeTab: {
      background: 'linear-gradient(to right, #3b82f6, #4f46e5)'
    },
    section: {
      background: 'white',
      borderRadius: '16px',
      padding: '32px',
      boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
      marginBottom: '32px'
    },
    sectionHeader: {
      display: 'flex',
      alignItems: 'center',
      gap: '16px',
      marginBottom: '24px'
    },
    sectionIcon: {
      padding: '12px',
      borderRadius: '12px'
    },
    zalopayIcon: {
      background: 'linear-gradient(to right, #f97316, #ef4444)'
    },
    stripeIcon: {
      background: 'linear-gradient(to right, #3b82f6, #4f46e5)'
    },
    sectionTitle: {
      fontSize: '30px',
      fontWeight: 'bold',
      color: '#1f2937',
      margin: 0
    },
    sectionSubtitle: {
      color: '#4b5563',
      margin: 0
    },
    featureGrid: {
      display: 'grid',
      gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
      gap: '24px',
      marginBottom: '32px'
    },
    featureCard: {
      borderRadius: '12px',
      padding: '24px',
      border: '1px solid'
    },
    zalopayFeature: {
      background: '#fff7ed',
      borderColor: '#fed7aa'
    },
    stripeFeature: {
      background: '#eff6ff',
      borderColor: '#bfdbfe'
    },
    featureTitle: {
      fontWeight: 'bold',
      marginBottom: '8px',
      fontSize: '16px'
    },
    zalopayFeatureTitle: {
      color: '#9a3412'
    },
    stripeFeatureTitle: {
      color: '#1e3a8a'
    },
    featureText: {
      fontSize: '14px'
    },
    zalopayFeatureText: {
      color: '#c2410c'
    },
    stripeFeatureText: {
      color: '#1e40af'
    },
    stepsTitle: {
      fontSize: '24px',
      fontWeight: 'bold',
      color: '#1f2937',
      marginBottom: '32px',
      display: 'flex',
      alignItems: 'center',
      gap: '12px'
    },
    stepNumber: {
      width: '32px',
      height: '32px',
      borderRadius: '50%',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      color: 'white',
      fontWeight: 'bold'
    },
    zalopayStepNumber: {
      background: 'linear-gradient(to right, #f97316, #ef4444)'
    },
    stripeStepNumber: {
      background: 'linear-gradient(to right, #3b82f6, #4f46e5)'
    },
    stepsContainer: {
      display: 'flex',
      flexDirection: 'column',
      gap: '24px'
    },
    stepCard: {
      display: 'flex',
      gap: '24px',
      padding: '24px',
      borderRadius: '12px',
      border: '1px solid',
      transition: 'box-shadow 0.3s ease',
      cursor: 'default'
    },
    zalopayStep: {
      background: 'linear-gradient(to right, #fff7ed, #fef2f2)',
      borderColor: '#fed7aa'
    },
    stripeStep: {
      background: 'linear-gradient(to right, #eff6ff, #eef2ff)',
      borderColor: '#bfdbfe'
    },
    stepNumberLarge: {
      width: '48px',
      height: '48px',
      borderRadius: '50%',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      color: 'white',
      fontWeight: 'bold',
      flexShrink: 0
    },
    stepContent: {
      flex: 1
    },
    stepTitle: {
      fontWeight: 'bold',
      color: '#1f2937',
      fontSize: '18px',
      marginBottom: '8px'
    },
    stepDesc: {
      color: '#4b5563',
      marginBottom: '16px'
    },
    actionButton: {
      color: 'white',
      padding: '8px 24px',
      borderRadius: '8px',
      fontWeight: '600',
      border: 'none',
      cursor: 'pointer',
      transition: 'box-shadow 0.3s ease',
      display: 'flex',
      alignItems: 'center',
      gap: '8px',
      fontSize: '14px'
    },
    zalopayButton: {
      background: 'linear-gradient(to right, #f97316, #ef4444)'
    },
    stripeButton: {
      background: 'linear-gradient(to right, #3b82f6, #4f46e5)'
    },
    keySection: {
      fontSize: '24px',
      fontWeight: 'bold',
      color: '#1f2937',
      marginBottom: '24px',
      display: 'flex',
      alignItems: 'center',
      gap: '12px'
    },
    keyGrid: {
      display: 'flex',
      flexDirection: 'column',
      gap: '24px'
    },
    keyCard: {
      background: '#f9fafb',
      borderRadius: '12px',
      padding: '24px',
      border: '2px dashed #d1d5db'
    },
    keyHeader: {
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: '12px'
    },
    keyLabel: {
      fontWeight: '600',
      color: '#374151'
    },
    keyActions: {
      display: 'flex',
      gap: '8px'
    },
    iconButton: {
      background: 'none',
      border: 'none',
      cursor: 'pointer',
      transition: 'color 0.3s ease'
    },
    zalopayIconButton: {
      color: '#f97316'
    },
    stripeIconButton: {
      color: '#3b82f6'
    },
    codeBlock: {
      background: 'white',
      padding: '8px 16px',
      borderRadius: '8px',
      fontFamily: 'monospace',
      fontSize: '14px',
      border: '1px solid #e5e7eb',
      wordBreak: 'break-all'
    },
    successMessage: {
      color: '#16a34a',
      fontSize: '14px',
      marginTop: '8px',
      display: 'flex',
      alignItems: 'center',
      gap: '4px'
    },
    warningCard: {
      background: '#fffbeb',
      borderRadius: '12px',
      padding: '24px',
      border: '1px solid #fde68a'
    },
    warningHeader: {
      display: 'flex',
      alignItems: 'flex-start',
      gap: '12px'
    },
    warningIcon: {
      color: '#d97706',
      flexShrink: 0,
      marginTop: '4px'
    },
    warningTitle: {
      fontWeight: 'bold',
      color: '#92400e',
      marginBottom: '8px'
    },
    warningList: {
      color: '#b45309',
      fontSize: '14px',
      margin: 0,
      paddingLeft: '16px'
    },
    supportSection: {
      fontSize: '24px',
      fontWeight: 'bold',
      color: '#1f2937',
      marginBottom: '24px',
      textAlign: 'center'
    },
    supportGrid: {
      display: 'grid',
      gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
      gap: '24px'
    },
    supportCard: {
      textAlign: 'center',
      padding: '24px',
      borderRadius: '12px',
      border: '1px solid'
    },
    zalopaySupport: {
      background: '#fff7ed',
      borderColor: '#fed7aa'
    },
    stripeSupport: {
      background: '#eff6ff',
      borderColor: '#bfdbfe'
    },
    supportIcon: {
      width: '48px',
      height: '48px',
      margin: '0 auto 16px'
    },
    zalopayIconColor: {
      color: '#f97316'
    },
    stripeIconColor: {
      color: '#3b82f6'
    },
    supportTitle: {
      fontWeight: 'bold',
      marginBottom: '8px'
    },
    zalopayText: {
      color: '#9a3412'
    },
    stripeText: {
      color: '#1e3a8a'
    },
    supportText: {
      fontSize: '14px',
      marginBottom: '16px'
    },
    zalopayTextColor: {
      color: '#c2410c'
    },
    stripeTextColor: {
      color: '#1e40af'
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <div style={styles.headerContent}>
          <div style={styles.headerCenter}>
            <div style={styles.headerIconContainer}>
              <div style={styles.headerIcon}>
                <Key style={{width: '32px', height: '32px', color: 'white'}} />
              </div>
              <h1 style={styles.title}>
                Hướng dẫn lấy Merchant Key
              </h1>
            </div>
            <p style={styles.subtitle}>
              Hướng dẫn chi tiết cách đăng ký và lấy merchant key từ ZaloPay và Stripe 
              để tích hợp thanh toán vào ứng dụng của bạn
            </p>
          </div>
        </div>
      </div>

      <div style={styles.main}>
        <div style={styles.tabContainer}>
          <div style={styles.tabWrapper}>
            <button
              onClick={() => setActiveTab('zalopay')}
              style={{
                ...styles.tabButton,
                ...(activeTab === 'zalopay' ? {...styles.tabButtonActive, ...styles.zalopayTab} : styles.tabButtonInactive)
              }}
            >
              <CreditCard style={{width: '20px', height: '20px'}} />
              ZaloPay
            </button>
            <button
              onClick={() => setActiveTab('stripe')}
              style={{
                ...styles.tabButton,
                ...(activeTab === 'stripe' ? {...styles.tabButtonActive, ...styles.stripeTab} : styles.tabButtonInactive)
              }}
            >
              <Shield style={{width: '20px', height: '20px'}} />
              Stripe
            </button>
          </div>
        </div>

        {activeTab === 'zalopay' && (
          <div>
            <div style={styles.section}>
              <div style={styles.sectionHeader}>
                <div style={{...styles.sectionIcon, ...styles.zalopayIcon}}>
                  <CreditCard style={{width: '32px', height: '32px', color: 'white'}} />
                </div>
                <div>
                  <h2 style={styles.sectionTitle}>ZaloPay Merchant</h2>
                  <p style={styles.sectionSubtitle}>Cổng thanh toán phổ biến tại Việt Nam</p>
                </div>
              </div>
              
              <div style={styles.featureGrid}>
                <div style={{...styles.featureCard, ...styles.zalopayFeature}}>
                  <h3 style={{...styles.featureTitle, ...styles.zalopayFeatureTitle}}>Miễn phí</h3>
                  <p style={{...styles.featureText, ...styles.zalopayFeatureText}}>Không tốn phí đăng ký tài khoản merchant</p>
                </div>
                <div style={{...styles.featureCard, ...styles.zalopayFeature}}>
                  <h3 style={{...styles.featureTitle, ...styles.zalopayFeatureTitle}}>Nhanh chóng</h3>
                  <p style={{...styles.featureText, ...styles.zalopayFeatureText}}>Duyệt trong 1-2 ngày làm việc</p>
                </div>
                <div style={{...styles.featureCard, ...styles.zalopayFeature}}>
                  <h3 style={{...styles.featureTitle, ...styles.zalopayFeatureTitle}}>Hỗ trợ 24/7</h3>
                  <p style={{...styles.featureText, ...styles.zalopayFeatureText}}>Đội ngũ support chuyên nghiệp</p>
                </div>
              </div>
            </div>

            <div style={styles.section}>
              <h3 style={styles.stepsTitle}>
                <div style={{...styles.stepNumber, ...styles.zalopayStepNumber}}>
                  1
                </div>
                Các bước đăng ký
              </h3>

              <div style={styles.stepsContainer}>
                {stepData.map((item, index) => (
                  <div key={index} style={{...styles.stepCard, ...styles.zalopayStep}}>
                    <div style={{...styles.stepNumberLarge, ...styles.zalopayStepNumber}}>
                      {index + 1}
                    </div>
                    <div style={styles.stepContent}>
                      <h4 style={styles.stepTitle}>{item.title}</h4>
                      <p style={styles.stepDesc}>{item.desc}</p>
                      {item.action && (
                        <button 
                          onClick={() => window.open(item.link, '_blank')}
                          style={{...styles.actionButton, ...styles.zalopayButton}}
                        >
                          {item.action}
                          <ExternalLink style={{width: '16px', height: '16px'}} />
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div style={styles.section}>
              <h3 style={styles.keySection}>
                <Key style={{width: '24px', height: '24px', color: '#f97316'}} />
                Thông tin merchant key mẫu
              </h3>
              
              <div style={styles.keyGrid}>
                <div style={styles.keyCard}>
                  <div style={styles.keyHeader}>
                    <label style={styles.keyLabel}>App ID:</label>
                    <button
                      onClick={() => copyToClipboard('2553', 'zalopay-appid')}
                      style={{...styles.iconButton, ...styles.zalopayIconButton}}
                    >
                      <Copy style={{width: '16px', height: '16px'}} />
                    </button>
                  </div>
                  <code style={styles.codeBlock}>
                    2553
                  </code>
                  {copiedKey === 'zalopay-appid' && (
                    <p style={styles.successMessage}>
                      <CheckCircle style={{width: '16px', height: '16px'}} /> Đã copy!
                    </p>
                  )}
                </div>

                <div style={styles.keyCard}>
                  <div style={styles.keyHeader}>
                    <label style={styles.keyLabel}>Key 1:</label>
                    <div style={styles.keyActions}>
                      <button
                        onClick={() => setShowApiKey(!showApiKey)}
                        style={styles.iconButton}
                      >
                        {showApiKey ? <EyeOff style={{width: '16px', height: '16px'}} /> : <Eye style={{width: '16px', height: '16px'}} />}
                      </button>
                      <button
                        onClick={() => copyToClipboard('PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL', 'zalopay-key1')}
                        style={{...styles.iconButton, ...styles.zalopayIconButton}}
                      >
                        <Copy style={{width: '16px', height: '16px'}} />
                      </button>
                    </div>
                  </div>
                  <code style={styles.codeBlock}>
                    {showApiKey ? 'PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL' : '•••••••••••••••••••••••••••••••••'}
                  </code>
                  {copiedKey === 'zalopay-key1' && (
                    <p style={styles.successMessage}>
                      <CheckCircle style={{width: '16px', height: '16px'}} /> Đã copy!
                    </p>
                  )}
                </div>

                <div style={styles.warningCard}>
                  <div style={styles.warningHeader}>
                    <Shield style={{width: '24px', height: '24px', ...styles.warningIcon}} />
                    <div>
                      <h4 style={styles.warningTitle}>Lưu ý bảo mật</h4>
                      <ul style={styles.warningList}>
                        <li>Không chia sẻ key với bất kỳ ai</li>
                        <li>Lưu trữ key trong biến môi trường</li>
                        <li>Chỉ sử dụng key trong môi trường server</li>
                      </ul>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {activeTab === 'stripe' && (
          <div>
            <div style={styles.section}>
              <div style={styles.sectionHeader}>
                <div style={{...styles.sectionIcon, ...styles.stripeIcon}}>
                  <Shield style={{width: '32px', height: '32px', color: 'white'}} />
                </div>
                <div>
                  <h2 style={styles.sectionTitle}>Stripe Connect</h2>
                  <p style={styles.sectionSubtitle}>Kết nối tài khoản với merchant platform</p>
                </div>
              </div>
              
              <div style={styles.featureGrid}>
                <div style={{...styles.featureCard, ...styles.stripeFeature}}>
                  <h3 style={{...styles.featureTitle, ...styles.stripeFeatureTitle}}>Kết nối nhanh</h3>
                  <p style={{...styles.featureText, ...styles.stripeFeatureText}}>Tự động kết nối với platform</p>
                </div>
                <div style={{...styles.featureCard, ...styles.stripeFeature}}>
                  <h3 style={{...styles.featureTitle, ...styles.stripeFeatureTitle}}>Quản lý tập trung</h3>
                  <p style={{...styles.featureText, ...styles.stripeFeatureText}}>Platform quản lý payments cho bạn</p>
                </div>
                <div style={{...styles.featureCard, ...styles.stripeFeature}}>
                  <h3 style={{...styles.featureTitle, ...styles.stripeFeatureTitle}}>Chia sẻ doanh thu</h3>
                  <p style={{...styles.featureText, ...styles.stripeFeatureText}}>Tự động split payments</p>
                </div>
              </div>
            </div>

            <div style={styles.section}>
              <h3 style={styles.stepsTitle}>
                <div style={{...styles.stepNumber, ...styles.stripeStepNumber}}>
                  1
                </div>
                Các bước kết nối tài khoản
              </h3>

              <div style={styles.stepsContainer}>
                {stripeStepData.map((item, index) => (
                  <div key={index} style={{...styles.stepCard, ...styles.stripeStep}}>
                    <div style={{...styles.stepNumberLarge, ...styles.stripeStepNumber}}>
                      {index + 1}
                    </div>
                    <div style={styles.stepContent}>
                      <h4 style={styles.stepTitle}>{item.title}</h4>
                      <p style={styles.stepDesc}>{item.desc}</p>
                      {item.action && (
                        <button 
                          onClick={() => window.open(item.link, '_blank')}
                          style={{...styles.actionButton, ...styles.stripeButton}}
                        >
                          {item.action}
                          <ExternalLink style={{width: '16px', height: '16px'}} />
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div style={styles.section}>
              <h3 style={styles.keySection}>
                <Key style={{width: '24px', height: '24px', color: '#3b82f6'}} />
                Thông tin sau khi kết nối
              </h3>
              
              <div style={styles.keyGrid}>
                <div style={styles.keyCard}>
                  <div style={styles.keyHeader}>
                    <label style={styles.keyLabel}>Account ID (Connected Account):</label>
                    <button
                      onClick={() => copyToClipboard('acct_1234567890abcdefgh', 'stripe-account')}
                      style={{...styles.iconButton, ...styles.stripeIconButton}}
                    >
                      <Copy style={{width: '16px', height: '16px'}} />
                    </button>
                  </div>
                  <code style={styles.codeBlock}>
                    acct_1234567890abcdefgh
                  </code>
                  {copiedKey === 'stripe-account' && (
                    <p style={styles.successMessage}>
                      <CheckCircle style={{width: '16px', height: '16px'}} /> Đã copy!
                    </p>
                  )}
                  <p style={{color: '#4b5563', fontSize: '14px', marginTop: '8px'}}>
                    <strong>Lưu ý:</strong> Đây là Account ID của tài khoản đã kết nối, platform sẽ sử dụng để xử lý payments cho bạn
                  </p>
                </div>

                <div style={styles.keyCard}>
                  <div style={styles.keyHeader}>
                    <label style={styles.keyLabel}>Access Token (Platform sử dụng):</label>
                    <div style={styles.keyActions}>
                      <button
                        onClick={() => setShowApiKey(!showApiKey)}
                        style={styles.iconButton}
                      >
                        {showApiKey ? <EyeOff style={{width: '16px', height: '16px'}} /> : <Eye style={{width: '16px', height: '16px'}} />}
                      </button>
                      <button
                        onClick={() => copyToClipboard('sk_test_51234567890_account_abcdefghijklmnopqrstuvwxyz', 'stripe-token')}
                        style={{...styles.iconButton, ...styles.stripeIconButton}}
                      >
                        <Copy style={{width: '16px', height: '16px'}} />
                      </button>
                    </div>
                  </div>
                  <code style={styles.codeBlock}>
                    {showApiKey ? 'sk_test_51234567890_account_abcdefghijklmnopqrstuvwxyz' : '•••••••••••••••••••••••••••••••••••••••••••••••••••••••'}
                  </code>
                  {copiedKey === 'stripe-token' && (
                    <p style={styles.successMessage}>
                      <CheckCircle style={{width: '16px', height: '16px'}} /> Đã copy!
                    </p>
                  )}
                  <p style={{color: '#4b5563', fontSize: '14px', marginTop: '8px'}}>
                    <strong>Lưu ý:</strong> Token này được platform sử dụng để thay mặt bạn xử lý payments
                  </p>
                </div>

                <div style={styles.warningCard}>
                  <div style={styles.warningHeader}>
                    <Shield style={{width: '24px', height: '24px', ...styles.warningIcon}} />
                    <div>
                      <h4 style={styles.warningTitle}>Về Stripe Connect</h4>
                      <ul style={styles.warningList}>
                        <li>Tài khoản của bạn được kết nối với platform merchant</li>
                        <li>Platform sẽ xử lý payments và chuyển tiền cho bạn</li>
                        <li>Bạn có thể thu hồi quyền truy cập bất cứ lúc nào</li>
                        <li>Kiểm tra dashboard Stripe để theo dõi giao dịch</li>
                      </ul>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div style={styles.section}>
              <h3 style={styles.stepsTitle}>
                <div style={{...styles.stepNumber, ...styles.stripeStepNumber}}>
                  <ChevronRight style={{width: '16px', height: '16px'}} />
                </div>
                Cách thức hoạt động của Stripe Connect
              </h3>
              
              <div style={styles.stepsContainer}>
                <div style={{...styles.stripeFeature, borderRadius: '12px', padding: '24px', border: '1px solid #bfdbfe'}}>
                  <h4 style={{fontWeight: 'bold', ...styles.stripeFeatureTitle, marginBottom: '12px'}}>1. Quy trình kết nối</h4>
                  <p style={{...styles.stripeFeatureText, marginBottom: '12px'}}>
                    Khi bạn nhấp vào link connect, bạn sẽ được chuyển đến Stripe để xác nhận việc cho phép platform 
                    truy cập tài khoản của bạn.
                  </p>
                  <div style={{background: 'white', borderRadius: '8px', padding: '16px', border: '1px solid #bfdbfe'}}>
                    <code style={{fontSize: '14px', color: '#374151'}}>
                      Platform → Stripe OAuth → Xác nhận quyền → Nhận Access Token
                    </code>
                  </div>
                </div>
                
                <div style={{...styles.stripeFeature, borderRadius: '12px', padding: '24px', border: '1px solid #bfdbfe'}}>
                  <h4 style={{fontWeight: 'bold', ...styles.stripeFeatureTitle, marginBottom: '12px'}}>2. Xử lý thanh toán</h4>
                  <p style={{...styles.stripeFeatureText, marginBottom: '12px'}}>
                    Platform sẽ tạo payments thay mặt bạn và có thể tự động chia sẻ doanh thu.
                  </p>
                  <div style={{background: 'white', borderRadius: '8px', padding: '16px', border: '1px solid #bfdbfe'}}>
                    <code style={{fontSize: '14px', color: '#374151'}}>
                      Customer Payment → Platform → Your Connected Account → Bank Account
                    </code>
                  </div>
                </div>

                <div style={{background: '#f0fdf4', borderRadius: '12px', padding: '24px', border: '1px solid #bbf7d0'}}>
                  <h4 style={{fontWeight: 'bold', color: '#166534', marginBottom: '8px'}}>✅ Lợi ích của Connect</h4>
                  <ul style={{color: '#15803d', fontSize: '14px', margin: 0, paddingLeft: '16px'}}>
                    <li>Không cần tự xây dựng payment system</li>
                    <li>Platform handle compliance và security</li>
                    <li>Tự động split payments và fees</li>
                    <li>Có thể theo dõi trong Stripe Dashboard</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        )}

        <div style={{...styles.section, marginTop: '48px'}}>
          <h3 style={styles.supportSection}>Cần hỗ trợ thêm?</h3>
          <div style={styles.supportGrid}>
            <div style={{...styles.supportCard, ...styles.zalopaySupport}}>
              <CreditCard style={{...styles.supportIcon, ...styles.zalopayIconColor}} />
              <h4 style={{...styles.supportTitle, ...styles.zalopayText}}>ZaloPay Support</h4>
              <p style={{...styles.supportText, ...styles.zalopayTextColor}}>Liên hệ đội ngũ hỗ trợ ZaloPay</p>
              <button 
                onClick={() => window.open('https://merchant.zalopay.vn/support', '_blank')}
                style={{...styles.actionButton, ...styles.zalopayButton}}
              >
                Liên hệ support
              </button>
            </div>
            <div style={{...styles.supportCard, ...styles.stripeSupport}}>
              <Shield style={{...styles.supportIcon, ...styles.stripeIconColor}} />
              <h4 style={{...styles.supportTitle, ...styles.stripeText}}>Stripe Connect Support</h4>
              <p style={{...styles.supportText, ...styles.stripeTextColor}}>Hỗ trợ về kết nối tài khoản</p>
              <button 
                onClick={() => window.open('https://stripe.com/docs/connect', '_blank')}
                style={{...styles.actionButton, ...styles.stripeButton}}
              >
                Tài liệu Connect
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}