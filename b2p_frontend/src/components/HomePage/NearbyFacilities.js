import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Card, List, Tag, Spin, Empty } from 'antd';
import { EnvironmentOutlined } from '@ant-design/icons';
import { calculateDistance, convertAddressToCoordinates } from '../../services/locationService';
import { getAllFacilitiesByPlayer } from '../../services/apiService'; // Import your service to fetch facilities

const NearbyCourts = ({ userLocation }) => {
  const [courts, setCourts] = useState([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    if (userLocation) {
      loadNearbyCourts();
    }
  }, [userLocation]);

  const loadNearbyCourts = async () => {
    setLoading(true);
    try {
      console.log('🔍 Loading facilities...');

      // ✅ BODY ĐÚNG FORMAT cho POST API
      const requestBody = {
        name: "", // empty để lấy tất cả
        type: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], // hoặc type nào bạn muốn filter
        city: "", // empty để lấy tất cả city
        ward: "", // empty để lấy tất cả ward
        order: 0 // default order
      };

      const response = await getAllFacilitiesByPlayer(1, 50, requestBody);

      if (response.data && response.data.items) {
        console.log('📦 Raw API data:', response.data.items);

        // ✅ CONVERT ADDRESS → COORDINATES
        const facilitiesWithCoords = [];

        for (let facility of response.data.items) {
          try {
            console.log(`🌐 Converting: ${facility.location}`);

            const coords = await convertAddressToCoordinates(facility.location);

            if (coords) {
              facilitiesWithCoords.push({
                ...facility,
                latitude: coords.lat,
                longitude: coords.lng
              });
              console.log(`✅ ${facility.facilityName}: ${coords.lat}, ${coords.lng}`);
            } else {
              console.log(`❌ Failed to convert: ${facility.location}`);
            }

            // Delay để không spam API
            await new Promise(resolve => setTimeout(resolve, 400));

          } catch (error) {
            console.error(`❌ Error converting ${facility.location}:`, error);
          }
        }

        // Tính khoảng cách
        const courtsWithDistance = facilitiesWithCoords.map(facility => {
          const distance = calculateDistance(
            userLocation.lat,
            userLocation.lng,
            facility.latitude,
            facility.longitude
          );

          return {
            ...facility,
            distance: distance,
            distanceText: distance < 1
              ? `${Math.round(distance * 1000)}m`
              : `${distance.toFixed(1)}km`
          };
        });

        // Sắp xếp theo khoảng cách
        const sortedCourts = courtsWithDistance
          .filter(court => court.distance <= 50) // 50km radius
          .sort((a, b) => a.distance - b.distance);

        console.log('🎯 Final nearby courts:', sortedCourts);
        setCourts(sortedCourts);

      } else {
        console.log('❌ No data in API response');
      }
    } catch (error) {
      console.error('❌ Error loading courts:', error);
    } finally {
      setLoading(false);
    }
  };

  // ... rest of component giữ nguyên (loading, empty, render)
  if (loading) {
    return (
      <Card title="🌍 Sân gần bạn">
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <Spin size="large" />
          <p style={{ marginTop: '16px' }}>Wait a moment...</p>
        </div>
      </Card>
    );
  }

  if (courts.length === 0) {
    return (
      <Card title="🌍 Sân gần bạn">
        <Empty description="Không convert được địa chỉ nào hoặc không có sân gần" />
      </Card>
    );
  }

  return (
    <Card title={`🌍 ${courts.length} sân gần bạn`}>
      <List
        dataSource={courts}
        renderItem={(court) => (
          <Link
            to={`/facility-details/${court.facilityId}`}
            style={{ textDecoration: 'none', color: 'inherit' }}
          >
            <List.Item
              key={court.facilityId}
              style={{ cursor: 'pointer' }}
              className="hover:bg-gray-50"
            >
              <List.Item.Meta
                avatar={
                  <div style={{
                    width: '48px',
                    height: '48px',
                    borderRadius: '8px',
                    background: '#1890ff',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: 'white'
                  }}>
                    ⚽
                  </div>
                }
                title={
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <span>{court.facilityName}</span>
                    <Tag color="blue">
                      {court.distanceText}
                    </Tag>
                  </div>
                }
                description={
                  <div>
                    <div style={{ color: '#666', marginBottom: '4px' }}>
                      <EnvironmentOutlined /> {court.location}
                    </div>
                    <div style={{ fontSize: '12px', color: '#999' }}>
                      📍 {court.latitude?.toFixed(4)}, {court.longitude?.toFixed(4)}
                    </div>
                  </div>
                }
              />
            </List.Item>
          </Link>
        )}
      />
    </Card>
  );
};

export default NearbyCourts;