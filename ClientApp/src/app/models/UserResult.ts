export interface UserResult {
    ID: string;
    Name: string;
    ScreenName: string;
    Location: string;
    Description: string;
    FollowerCount: number; // long
    FriendCount: number;  // long
    CreatedAt: string;
    TimeZone: string;
    GeoEnabled: boolean;
    Verified: boolean;
    StatusCount: number; // long
    Lang: string;
    ProfileImage: string;
}


export class UserConnectionInfo {
    distance: number;
    connections: UserResult[];
}

export interface UserConnectionMap {
    [key: string]: UserConnectionInfo;
}
