import { Injectable } from '@angular/core';
import { WeightedWord } from '../cloud-bottle/cloud-bottle.component';
import { EndpointService } from './endpoint.service';
import { Country, PlaceResult } from '../models';
import { Observable } from 'rxjs/Observable';
import * as D3 from 'd3';
@Injectable()
export class CloudDataService {

    allWords: D3.Map<WeightedWord> = D3.map<WeightedWord>();
    wordsAdded: number;

    constructor(private endpoint: EndpointService) {

    }

    getRelatedHashes(hashtag: string): Observable<WeightedWord[]> {
        return this.endpoint.searchRelatedHashtags(hashtag).map((results: string[]): WeightedWord[] => {

            const oldCount = this.allWords.size();

            results.forEach((hash) => {
                if (this.allWords.has(hash)) {
                    this.allWords.get(hash).occurrence++;
                } else {
                    this.allWords.set(hash, { text: hash, size: 10, occurrence: 1 });
                }
            });

            this.wordsAdded = this.allWords.size() - oldCount;
            this.allWords.each((word) => {

                const limiter = (this.allWords.size() > 100) ? this.allWords.size() / 100 : 1;
                word.size = 10  + (Math.random() * 90)  / limiter;
            });

            return this.allWords.values();
        });
    }

    getTotalWordCount(): number {
        return this.allWords.size();
    }

    dropCachedWords(): void {
        this.allWords.clear();
    }

    getWordsAdded(): number {
        return this.wordsAdded;
    }



    init(): WeightedWord[] {
        return ['Hello', 'world', 'normally', 'you', 'want', 'more', 'words', 'than', 'this',
            'lorem', 'ipsum', 'dolor', 'sit', 'amet', 'consectetur',
            'adipiscing', 'elit', 'curabitur', 'vel', 'hendrerit', 'libero',
            'eleifend', 'blandit', 'nunc', 'ornare', 'odio', 'ut',
            'orci', 'gravida', 'imperdiet', 'nullam', 'purus', 'lacinia',
            'a', 'pretium', 'quis', 'congue', 'praesent', 'sagittis',
            'laoreet', 'auctor', 'mauris', 'non', 'velit', 'eros',
            'dictum', 'proin', 'accumsan', 'sapien', 'nec', 'massa',
            'volutpat', 'venenatis', 'sed', 'eu', 'molestie', 'lacus',
            'quisque', 'porttitor', 'ligula', 'dui', 'mollis', 'tempus',
            'at', 'magna', 'vestibulum', 'turpis', 'ac', 'diam',
            'tincidunt', 'id', 'condimentum', 'enim', 'sodales', 'in',
            'hac', 'habitasse', 'platea', 'dictumst', 'aenean', 'neque',
            'fusce', 'augue', 'leo', 'eget', 'semper', 'mattis',
            'tortor', 'scelerisque', 'nulla', 'interdum', 'tellus', 'malesuada',
            'rhoncus', 'porta', 'sem', 'aliquet', 'et', 'nam',
            'suspendisse', 'potenti', 'vivamus', 'luctus', 'fringilla', 'erat',
            'donec', 'justo', 'vehicula', 'ultricies', 'varius', 'ante',
            'primis', 'faucibus', 'ultrices', 'posuere', 'cubilia', 'curae',
            'etiam', 'cursus', 'aliquam', 'quam', 'dapibus', 'nisl',
            'feugiat', 'egestas', 'class', 'aptent', 'taciti', 'sociosqu',
            'ad', 'litora', 'torquent', 'per', 'conubia', 'nostra',
            'inceptos', 'himenaeos', 'phasellus', 'nibh', 'pulvinar', 'vitae',
            'urna', 'iaculis', 'lobortis', 'nisi', 'viverra', 'arcu',
            'morbi', 'pellentesque', 'metus', 'commodo', 'ut', 'facilisis',
            'felis', 'tristique', 'ullamcorper', 'placerat', 'aenean', 'convallis',
            'sollicitudin', 'integer', 'rutrum', 'duis', 'est', 'etiam',
            'bibendum', 'donec', 'pharetra', 'vulputate', 'maecenas', 'mi',
            'fermentum', 'consequat', 'suscipit', 'aliquam', 'habitant', 'senectus',
            'netus', 'fames', 'quisque', 'euismod', 'curabitur', 'lectus',
            'elementum', 'tempor', 'risus', 'cras']
            .map(function (d): WeightedWord {
                return { text: d, size: 10 + Math.random() * 90, occurrence: 1 };
            });
    }
}
