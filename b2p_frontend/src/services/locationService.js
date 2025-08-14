export const getCurrentLocation = () => {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject(new Error('Trình duyệt không hỗ trợ định vị'));
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const location = {
          lat: position.coords.latitude,
          lng: position.coords.longitude
        };
        resolve(location);
      },
      (error) => {
        reject(new Error('Không thể lấy vị trí'));
      }
    );
  });
};
export const convertAddressToCoordinates = async (address) => {
  try {
    console.log(`🌐 Converting address: ${address}`);
    
    // Thêm delay để không spam API
    await new Promise(resolve => setTimeout(resolve, 300));
    
    const response = await fetch(
      `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(address)}&countrycodes=vn&limit=1&addressdetails=1`,
      {
        headers: {
          'User-Agent': 'FootballBooking/1.0 (vuhongson283@example.com)' // Required by Nominatim
        }
      }
    );
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    const data = await response.json();
    
    if (data && data.length > 0) {
      const result = data[0];
      console.log(`✅ Converted "${address}" → ${result.lat}, ${result.lon}`);
      
      return {
        lat: parseFloat(result.lat),
        lng: parseFloat(result.lon),
        formatted_address: result.display_name
      };
    } else {
      console.error(`❌ No results for "${address}"`);
      return null;
    }
  } catch (error) {
    console.error(`❌ Error converting address "${address}":`, error);
    return null;
  }
};
// ✅ THÊM function tính khoảng cách
export const calculateDistance = (lat1, lng1, lat2, lng2) => {
  const R = 6371; // Bán kính Trái Đất (km)
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLng = (lng2 - lng1) * Math.PI / 180;
  
  const a = 
    Math.sin(dLat/2) * Math.sin(dLat/2) +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * 
    Math.sin(dLng/2) * Math.sin(dLng/2);
    
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
  const distance = R * c; // Khoảng cách (km)
  
  return distance;
};