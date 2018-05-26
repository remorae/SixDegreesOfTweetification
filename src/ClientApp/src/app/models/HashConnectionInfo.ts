export class HashConnectionInfo {
    distance: number;
    connections: string[];
}

export interface HashConnectionMap {
    [query: string]: HashConnectionInfo;
}
