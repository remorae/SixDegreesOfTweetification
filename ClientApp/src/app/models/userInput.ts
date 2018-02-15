export class UserInput {
    public inputType: string; // # | @
    inputs: string[];
    constructor(type: string, ...inputs: string[]) {
        this.inputType = type;
        this.inputs = inputs;
    }


}
export type HashOrAt = '#' | '@';
