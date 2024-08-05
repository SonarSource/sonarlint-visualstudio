try {
    func();
} catch (ex) {  // Noncompliant
    throw ex;
}

function func() { }

const CONSTANT_1 = 1;
const CONSTANT_2 = 2;
const CONSTANT_3 = 3;
const CONSTANT_4 = 4;
const CONSTANT_5 = 5;
const CONSTANT_6 = 6;
const CONSTANT_7 = 7;
const CONSTANT_8 = 8;
const CONSTANT_9 = 9;
const CONSTANT_10 = 10;
const CONSTANT_11 = 11;
const CONSTANT_12 = 12;
const CONSTANT_13 = 13;
const CONSTANT_14 = 14;
const CONSTANT_15 = 15;
const CONSTANT_16 = 16;
const CONSTANT_17 = 17;
const CONSTANT_18 = 18;
const CONSTANT_19 = 19;
const CONSTANT_20 = 20;

function calculate(input: number): string {
    if (input === CONSTANT_1) {
        return "Input is CONSTANT_1";
    } else if (input === CONSTANT_2) {
        return "Input is CONSTANT_2";
    } else if (input === CONSTANT_3) {
        return "Input is CONSTANT_3";
    } else if (input === CONSTANT_4) {
        return "Input is CONSTANT_4";
    } else if (input === CONSTANT_5) {
        return "Input is CONSTANT_5";
    } else if (input === CONSTANT_6) {
        return "Input is CONSTANT_6";
    } else if (input === CONSTANT_7) {
        return "Input is CONSTANT_7";
    } else if (input === CONSTANT_8) {
        return "Input is CONSTANT_8";
    } else if (input === CONSTANT_9) {
        return "Input is CONSTANT_9";
    } else if (input === CONSTANT_10) {
        return "Input is CONSTANT_10";
    } else if (input === CONSTANT_11) {
        return "Input is CONSTANT_11";
    } else if (input === CONSTANT_12) {
        return "Input is CONSTANT_12";
    } else if (input === CONSTANT_13) {
        return "Input is CONSTANT_13";
    } else if (input === CONSTANT_14) {
        return "Input is CONSTANT_14";
    } else if (input === CONSTANT_15) {
        return "Input is CONSTANT_15";
    } else if (input === CONSTANT_16) {
        return "Input is CONSTANT_16";
    } else if (input === CONSTANT_17) {
        return "Input is CONSTANT_17";
    } else if (input === CONSTANT_18) {
        return "Input is CONSTANT_18";
    } else if (input === CONSTANT_19) {
        return "Input is CONSTANT_19";
    } else if (input === CONSTANT_20) {
        return "Input is CONSTANT_20";
    } else {
        return "Input does not match any constant";
    }
}

function func2(key, iv) {
    const ip = "192.168.12.42"; // Sensitive
    console.log(ip);
};