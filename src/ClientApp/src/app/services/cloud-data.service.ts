import { Injectable } from '@angular/core';
import { WeightedWord } from '../cloud-bottle/cloud-bottle.component';
import { EndpointService } from './endpoint.service';
import { Country, PlaceResult } from '../models';
import { Observable } from 'rxjs/Observable';
import { map } from 'rxjs/operators/map';
import { Map as D3Map, map as d3map } from 'd3';
/**
 * @example Fetches and transforms the data associated with the Word Cloud
 */
@Injectable()
export class CloudDataService {
    allWords: D3Map<WeightedWord> = d3map<WeightedWord>();
    wordsAdded: number;

    constructor(private endpoint: EndpointService) {}

    /** @example Queries the backend for  associated words, filters out existing words and increments
     *      their occurrence, then sizes each word based on occurence.
     * @returns Returns an Observable that emits the latest words associated with the latest hashtag.
     * @param hashtag The hashtag input by the user.
     */
    getRelatedHashes(hashtag: string): Observable<WeightedWord[]> {
        return this.endpoint.searchRelatedHashtags(hashtag).pipe(
            map((results: string[]): WeightedWord[] => {
                const oldCount = this.allWords.size();

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
                });

                this.wordsAdded = this.allWords.size() - oldCount;
                this.allWords.each(word => {
                    const limiter =
                        this.allWords.size() > 100
                            ? this.allWords.size() / 100
                            : 1;
                    word.size = 10 + Math.random() * 90 / limiter;
                });

                return this.allWords.values();
            })
        );
    }
    /**
     * @returns The total number of words in the map.
     */
    getTotalWordCount(): number {
        return this.allWords.size();
    }
    /**
     * @returns Removes all existing words in the map.
     */
    dropCachedWords(): void {
        this.allWords.clear();
    }
    /**
     * @returns Get how many new words were added to the map.
     */
    getWordsAdded(): number {
        return this.wordsAdded;
    }
}
