export interface UserResult {
    id: string;
    name: string;
    screenName: string;
    location: string;
    description: string;
    followerCount: number; // long
    friendCount: number; // long
    createdAt: string;
    timeZone: string;
    geoEnabled: boolean;
    verified: boolean;
    statusCount: number; // long
    lang: string;
    profileImage: string;
}

export class UserConnectionInfo {
    distance: number;
    connections: UserResult[];
}

export interface UserConnectionMap {
    [key: string]: UserConnectionInfo;
}
