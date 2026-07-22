export const environment = {
  production: false,
  /**
   * The HTTP profile of CareConnect.Api. Switch to https://localhost:7122 if you prefer to
   * run the API over TLS - remember to trust the dev certificate first
   * (`dotnet dev-certs https --trust`).
   */
  apiBaseUrl: 'https://localhost:7122/api',
};
