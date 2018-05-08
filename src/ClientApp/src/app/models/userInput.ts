export class UserInput {
    public inputType: HashOrHandle;
    inputs: string[];
    constructor(type: HashOrHandle, ...inputs: string[]) {
        this.inputType = type;
        this.inputs = inputs;
    }
}
export type HashOrHandle = 'hashtag' | 'handle';

