import React from 'react';
import './ConfirmModal.scss';

const ConfirmModal = ({
    isOpen,
    onClose,
    onConfirm,
    title = "Xác nhận",
    message = "Bạn có chắc chắn muốn thực hiện hành động này?",
    confirmText = "Xác nhận",
    cancelText = "Hủy",
    type = "warning" // warning, danger, info
}) => {
    if (!isOpen) return null;

    const handleConfirm = () => {
        onConfirm();
        onClose();
    };

    const handleCancel = () => {
        onClose();
    };

    const getIcon = () => {
        switch (type) {
            case 'danger':
                return (
                    <svg className="confirm-icon danger" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.854-.833-2.598 0L3.232 16.5c-.77.833.192 2.5 1.732 2.5z" />
                    </svg>
                );
            case 'info':
                return (
                    <svg className="confirm-icon info" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                );
            default: // warning
                return (
                    <svg className="confirm-icon warning" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.854-.833-2.598 0L3.232 16.5c-.77.833.192 2.5 1.732 2.5z" />
                    </svg>
                );
        }
    };

    return (
        <div className="confirm-modal-overlay" onClick={handleCancel}>
            <div className="confirm-modal-content" onClick={(e) => e.stopPropagation()}>
                <div className="confirm-modal-header">
                    <div className="confirm-icon-wrapper">
                        {getIcon()}
                    </div>
                    <h3 className="confirm-title">{title}</h3>
                </div>

                <div className="confirm-modal-body">
                    <p className="confirm-message">{message}</p>
                </div>

                <div className="confirm-modal-footer">
                    <button
                        className="btn btn-cancel"
                        onClick={handleCancel}
                    >
                        {cancelText}
                    </button>
                    <button
                        className={`btn btn-confirm ${type}`}
                        onClick={handleConfirm}
                    >
                        {confirmText}
                    </button>
                </div>
            </div>
        </div>
    );
};

export default ConfirmModal;