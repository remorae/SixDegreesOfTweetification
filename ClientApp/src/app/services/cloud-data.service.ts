import { Injectable } from '@angular/core';
import { WeightedWord } from '../cloud-bottle/cloud-bottle.component';
import { EndpointService } from './endpoint.service';
import { Country, PlaceResult } from '../models';
import { Observable } from 'rxjs/Observable';
import { map } from 'rxjs/operators/map';
import { Map as D3Map, map as d3map } from 'd3';
@Injectable()
export class CloudDataService {

    allWords: D3Map<WeightedWord> = d3map<WeightedWord>();
    currentWords: D3Map<WeightedWord> = d3map<WeightedWord>();
    wordsAdded: number;

    constructor(private endpoint: EndpointService) {}

    getRelatedHashes(hashtag: string): Observable<WeightedWord[]> {
        return this.endpoint.searchRelatedHashtags(hashtag).pipe(
            map((results: string[]): WeightedWord[] => {
                const oldCount = this.allWords.size();
                this.currentWords.clear();

                results.forEach(hash => {
                    if (this.allWords.has(hash)) {
                        this.allWords.get(hash).occurrence++;
                    } else {
                        this.allWords.set(hash, {
                            text: hash,
                            size: 10,
                            occurrence: 1
                        });
                    }
                    if (this.currentWords.has(hash)) {
                        this.currentWords.get(hash).occurrence++;
                    } else {
                        this.currentWords.set(hash, { text: hash, size: 10, occurrence: 1 });
                    }
                });

                this.wordsAdded = this.allWords.size() - oldCount;
                this.allWords.each(word => {
                    const limiter =
                        this.allWords.size() > 100
                            ? this.allWords.size() / 100
                            : 1;
                    word.size = 10 + Math.random() * 90 / limiter;
                });
                this.currentWords.each((word) => {
                    const limiter =
                        this.currentWords.size() > 100
                            ? this.currentWords.size() / 100
                            : 1;
                    word.size = 10 + (Math.random() * 90)  / limiter;
                });

                return this.allWords.values();
            })
        );
    }

    getTotalWordCount(): number {
        return this.allWords.size();
    }

    dropCachedWords(): void {
        this.allWords.clear();
        this.currentWords.clear();
    }

    getWordsAdded(): number {
        return this.wordsAdded;
    }

    getCurrentWords(): WeightedWord[] {
        return this.currentWords.values();
    }
}
