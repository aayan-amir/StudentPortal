import { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.studentportal.app',
  appName: 'Student Portal',
  webDir: 'www',
  server: {
    url: 'https://studentportal-dd0caad7923b.herokuapp.com/',
    allowNavigation: ['studentportal-dd0caad7923b.herokuapp.com']
  }
};

export default config;
