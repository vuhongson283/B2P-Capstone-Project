import React, { useEffect, useRef, useState } from 'react';
import './HeaderSpacer.scss';

const HeaderSpacer = ({ headerSelector = '.custom-navbar' }) => {
    const [headerHeight, setHeaderHeight] = useState(0);
    const resizeObserverRef = useRef(null);

    useEffect(() => {
        const headerElement = document.querySelector(headerSelector);
        
        if (!headerElement) {
            console.warn(`Header element with selector "${headerSelector}" not found`);
            return;
        }

        const updateHeight = () => {
            const height = headerElement.offsetHeight;
            setHeaderHeight(height);
        };

        // Use ResizeObserver for optimal performance
        if ('ResizeObserver' in window) {
            resizeObserverRef.current = new ResizeObserver((entries) => {
                for (const entry of entries) {
                    setHeaderHeight(entry.contentRect.height);
                }
            });
            resizeObserverRef.current.observe(headerElement);
        } else {
            // Fallback for older browsers
            updateHeight();
            const handleResize = () => {
                clearTimeout(window.headerResizeTimer);
                window.headerResizeTimer = setTimeout(updateHeight, 100);
            };
            
            window.addEventListener('resize', handleResize);
            
            return () => {
                window.removeEventListener('resize', handleResize);
                clearTimeout(window.headerResizeTimer);
            };
        }

        // Initial height calculation
        updateHeight();

        // Cleanup ResizeObserver
        return () => {
            if (resizeObserverRef.current) {
                resizeObserverRef.current.disconnect();
            }
        };
    }, [headerSelector]);

    return (
        <div 
            className="header-spacer"
            style={{ height: `${headerHeight}px` }}
            aria-hidden="true"
        />
    );
};

export default HeaderSpacer;